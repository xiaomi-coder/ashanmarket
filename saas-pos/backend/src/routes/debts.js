import express from 'express'
import { PrismaClient } from '@prisma/client'
import { authMiddleware } from '../middleware/auth.js'

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
        userId: req.user.id,
        cashierName: req.user.username,
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

export default router
