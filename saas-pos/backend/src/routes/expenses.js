import express from 'express'
import { PrismaClient } from '@prisma/client'
import { authMiddleware } from '../middleware/auth.js'
import { apiKeyAuth } from '../middleware/apiKeyAuth.js'

const router = express.Router()
const prisma = new PrismaClient()

// Xarajatlarni yuklash
router.get('/', authMiddleware, async (req, res) => {
  try {
    const expenses = await prisma.expense.findMany({
      where: { tenantId: req.user.tenantId },
      orderBy: { date: 'desc' },
      take: 50 // oxirgi 50 ta
    })
    res.json(expenses)
  } catch (error) {
    console.error(error)
    res.status(500).json({ error: 'Xarajatlarni yuklashda xatolik' })
  }
})

// Yangi xarajat qo'shish
router.post('/', authMiddleware, async (req, res) => {
  try {
    const { reason, categoryName, amount } = req.body

    if (!reason || !categoryName || !amount) {
      return res.status(400).json({ error: "Barcha maydonlarni to'ldiring" })
    }

    const expense = await prisma.expense.create({
      data: {
        tenantId: req.user.tenantId,
        userId: req.user.userId,
        cashierName: req.user.fullName || 'Admin',
        reason,
        categoryName,
        amount: parseFloat(amount)
      }
    })

    res.status(201).json(expense)
  } catch (error) {
    console.error(error)
    res.status(500).json({ error: "Xarajat qo'shishda xatolik" })
  }
})

// ─── EXE Sync routes (API Key) ────────────────────────────────────────────────
router.use('/sync', apiKeyAuth)

// POST /api/expenses/sync/upload
router.post('/sync/upload', async (req, res) => {
  try {
    const { expenses } = req.body
    if (!expenses || !expenses.length) return res.json({ success: true })
    
    let count = 0
    for (const e of expenses) {
      const exists = await prisma.expense.findFirst({
        where: { tenantId: req.tenant.id, amount: e.amount, reason: e.reason || '' }
      })
      if (!exists) {
        await prisma.expense.create({
          data: {
            tenantId: req.tenant.id,
            amount: e.amount,
            reason: e.reason || 'Sync',
            categoryName: 'Umumiy',
            cashierName: 'Sync',
            userId: 1
          }
        })
        count++
      }
    }
    res.json({ success: true, count })
  } catch(e) {
    console.error('Expense sync error:', e)
    res.status(500).json({ error: e.message })
  }
})

export default router
