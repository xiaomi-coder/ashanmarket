import express from 'express'
import { PrismaClient } from '@prisma/client'
import { authMiddleware } from '../middleware/auth.js'
import { apiKeyAuth } from '../middleware/apiKeyAuth.js'

const router = express.Router()
const prisma = new PrismaClient()

// Qarzdorlarni yuklash
router.get('/', authMiddleware, async (req, res) => {
  try {
    const customers = await prisma.customer.findMany({
      where: { tenantId: req.user.tenantId, totalDebt: { gt: 0 } },
      orderBy: { name: 'asc' }
    })
    res.json(customers)
  } catch (error) {
    console.error(error)
    res.status(500).json({ error: 'Qarzdorlarni yuklashda xatolik' })
  }
})

// Qarz qo'shish yoki to'lash
router.post('/', authMiddleware, async (req, res) => {
  try {
    const { name, phone, amount, type } = req.body // type: 'borrow' yoki 'repay'

    if (!name || !amount || !type) {
      return res.status(400).json({ error: "Barcha maydonlarni to'ldiring" })
    }

    const numericAmount = parseFloat(amount)

    // Mijozni topamiz yoki yaratamiz
    let customer = await prisma.customer.findFirst({
      where: { tenantId: req.user.tenantId, name: name }
    })

    if (!customer) {
      customer = await prisma.customer.create({
        data: {
          tenantId: req.user.tenantId,
          name: name,
          phone: phone || null,
          totalDebt: 0
        }
      })
    }

    // Tranzaksiyani yaratamiz
    const transaction = await prisma.debtTransaction.create({
      data: {
        customerId: customer.id,
        userId: req.user.userId,
        cashierName: req.user.fullName || 'Admin',
        type: type,
        amount: numericAmount
      }
    })

    // Mijozning umumiy qarzini yangilaymiz
    const newTotalDebt = type === 'borrow' 
      ? customer.totalDebt + numericAmount 
      : customer.totalDebt - numericAmount

    await prisma.customer.update({
      where: { id: customer.id },
      data: { totalDebt: newTotalDebt }
    })

    res.status(201).json({ message: "Muvaffaqiyatli saqlandi", transaction })
  } catch (error) {
    console.error(error)
    res.status(500).json({ error: "Qarz amaliyotida xatolik" })
  }
})

// ─── EXE Sync routes (API Key) ────────────────────────────────────────────────
router.use('/sync', apiKeyAuth)

// POST /api/debts/sync/upload
router.post('/sync/upload', async (req, res) => {
  try {
    const { debts } = req.body
    if (!debts || !debts.length) return res.json({ success: true })
    
    let count = 0
    for (const d of debts) {
      let cust = await prisma.customer.findFirst({
        where: { tenantId: req.tenant.id, phone: d.phone }
      })
      if (!cust) {
        cust = await prisma.customer.create({
          data: {
            tenantId: req.tenant.id,
            phone: d.phone || 'N/A',
            name: d.name || 'Nomalum',
            totalDebt: d.debtBalance || 0
          }
        })
      } else {
        await prisma.customer.update({
          where: { id: cust.id },
          data: { totalDebt: d.debtBalance || 0 }
        })
      }
      count++
    }
    res.json({ success: true, count })
  } catch(e) {
    console.error('Debt sync error:', e)
    res.status(500).json({ error: e.message })
  }
})

export default router
