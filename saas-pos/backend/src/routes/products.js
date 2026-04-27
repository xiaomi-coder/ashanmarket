import { Router } from 'express'
import prisma from '../lib/prisma.js'
import { authMiddleware, adminOnly, apiKeyAuth } from '../middleware/auth.js'

const router = Router()

// ─── Web routes (JWT) ─────────────────────────────────────────────────────────
router.use('/web', authMiddleware)

// GET /api/products/web
router.get('/web', async (req, res) => {
  try {
    const { search } = req.query
    const products = await prisma.product.findMany({
      where: {
        tenantId: req.tenant.id,
        isActive: true,
        ...(search && {
          OR: [
            { name: { contains: search, mode: 'insensitive' } },
            { barcode: { contains: search } }
          ]
        })
      },
      include: { category: { select: { name: true, color: true } } },
      orderBy: { name: 'asc' },
      take: 100
    })
    res.json(products)
  } catch (err) {
    res.status(500).json({ error: err.message })
  }
})

// GET /api/products/web/barcode/:barcode
router.get('/web/barcode/:barcode', async (req, res) => {
  try {
    const product = await prisma.product.findUnique({
      where: { tenantId_barcode: { tenantId: req.tenant.id, barcode: req.params.barcode } },
      include: { category: { select: { name: true } } }
    })
    if (!product || !product.isActive) return res.status(404).json({ error: 'Mahsulot topilmadi' })
    res.json(product)
  } catch (err) {
    res.status(500).json({ error: err.message })
  }
})

// GET /api/products/web/categories
router.get('/web/categories', async (req, res) => {
  try {
    const cats = await prisma.category.findMany({
      where: { tenantId: req.tenant.id, isActive: true },
      orderBy: { name: 'asc' }
    })
    res.json(cats)
  } catch (err) {
    res.status(500).json({ error: err.message })
  }
})

// GET /api/products/web/low-stock
router.get('/web/low-stock', authMiddleware, adminOnly, async (req, res) => {
  try {
    const products = await prisma.product.findMany({
      where: {
        tenantId: req.tenant.id,
        isActive: true,
        stock: { lte: prisma.product.fields.lowStockThreshold }
      }
    })
    res.json(products)
  } catch (err) {
    res.status(500).json({ error: err.message })
  }
})

// POST /api/products/web
router.post('/web', adminOnly, async (req, res) => {
  try {
    const { barcode, name, price, costPrice, stock, lowStockThreshold, categoryId, unit } = req.body
    const product = await prisma.product.create({
      data: {
        tenantId: req.tenant.id,
        barcode, name,
        price: +price,
        costPrice: +(costPrice || 0),
        stock: +(stock || 0),
        lowStockThreshold: +(lowStockThreshold || 10),
        categoryId: categoryId ? +categoryId : null,
        unit: unit || 'dona'
      }
    })
    res.status(201).json(product)
  } catch (err) {
    if (err.code === 'P2002') return res.status(400).json({ error: 'Bu barcode allaqachon mavjud' })
    res.status(500).json({ error: err.message })
  }
})

// PUT /api/products/web/:id
router.put('/web/:id', adminOnly, async (req, res) => {
  try {
    const { barcode, name, price, costPrice, stock, lowStockThreshold, categoryId, unit } = req.body
    const product = await prisma.product.update({
      where: { id: +req.params.id },
      data: {
        barcode, name,
        price: +price,
        costPrice: +(costPrice || 0),
        stock: +(stock || 0),
        lowStockThreshold: +(lowStockThreshold || 10),
        categoryId: categoryId ? +categoryId : null,
        unit: unit || 'dona'
      }
    })
    res.json(product)
  } catch (err) {
    res.status(500).json({ error: err.message })
  }
})

// DELETE /api/products/web/:id
router.delete('/web/:id', adminOnly, async (req, res) => {
  try {
    await prisma.product.update({
      where: { id: +req.params.id },
      data: { isActive: false }
    })
    res.json({ message: "O'chirildi" })
  } catch (err) {
    res.status(500).json({ error: err.message })
  }
})

// ─── EXE Sync routes (API Key) ────────────────────────────────────────────────
router.use('/sync', apiKeyAuth)

// GET /api/products/sync — EXE barcha mahsulotlarni oladi
router.get('/sync', async (req, res) => {
  try {
    const since = req.query.since ? new Date(req.query.since) : new Date(0)
    const products = await prisma.product.findMany({
      where: { tenantId: req.tenant.id, isActive: true, updatedAt: { gte: since } },
      include: { category: { select: { name: true } } }
    })
    res.json({ products, tenantName: req.tenant.name, logoUrl: req.tenant.logoUrl })
  } catch (err) {
    res.status(500).json({ error: err.message })
  }
})

// GET /api/products/sync/barcode/:barcode — EXE barcode scan
router.get('/sync/barcode/:barcode', async (req, res) => {
  try {
    const product = await prisma.product.findUnique({
      where: { tenantId_barcode: { tenantId: req.tenant.id, barcode: req.params.barcode } }
    })
    if (!product || !product.isActive) return res.status(404).json({ error: 'Topilmadi' })
    res.json(product)
  } catch (err) {
    res.status(500).json({ error: err.message })
  }
})

export default router
