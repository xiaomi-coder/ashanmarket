import express from 'express'
import { PrismaClient } from '@prisma/client'
import { authenticateToken } from '../middleware/auth.js'

const router = express.Router()
const prisma = new PrismaClient()

// Xarajatlarni yuklash
router.get('/', authenticateToken, async (req, res) => {
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
router.post('/', authenticateToken, async (req, res) => {
  try {
    const { reason, categoryName, amount } = req.body

    if (!reason || !categoryName || !amount) {
      return res.status(400).json({ error: "Barcha maydonlarni to'ldiring" })
    }

    const expense = await prisma.expense.create({
      data: {
        tenantId: req.user.tenantId,
        userId: req.user.id,
        cashierName: req.user.username,
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

export default router
