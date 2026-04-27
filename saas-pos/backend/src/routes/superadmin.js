import { Router } from 'express'
import bcrypt from 'bcryptjs'
import prisma from '../lib/prisma.js'
import { superAdminAuth } from '../middleware/auth.js'

const router = Router()
router.use(superAdminAuth)

// ─── Barcha tenantlar ─────────────────────────────────────────────────────────
// GET /api/super/tenants
router.get('/tenants', async (req, res) => {
  try {
    const tenants = await prisma.tenant.findMany({
      orderBy: { createdAt: 'desc' },
      include: {
        _count: { select: { users: true, products: true, sales: true } }
      }
    })

    // Har bir tenant uchun bugungi savdo
    const result = await Promise.all(tenants.map(async (t) => {
      const todaySales = await prisma.sale.aggregate({
        where: {
          tenantId: t.id,
          createdAt: { gte: new Date(new Date().setHours(0,0,0,0)) }
        },
        _sum: { total: true },
        _count: true
      })
      return {
        ...t,
        todayRevenue: todaySales._sum.total || 0,
        todayTransactions: todaySales._count
      }
    }))

    res.json(result)
  } catch (err) {
    res.status(500).json({ error: err.message })
  }
})

// ─── Yangi tenant yaratish ────────────────────────────────────────────────────
// POST /api/super/tenants
router.post('/tenants', async (req, res) => {
  try {
    const { name, slug, phone, address, plan, months, adminUsername, adminPassword } = req.body

    if (!name || !slug || !adminUsername || !adminPassword) {
      return res.status(400).json({ error: "Barcha maydonlar to'ldirilishi kerak" })
    }

    // Slug tekshirish
    const existing = await prisma.tenant.findUnique({ where: { slug } })
    if (existing) return res.status(400).json({ error: "Bu slug allaqachon ishlatilgan" })

    const expiresAt = new Date()
    expiresAt.setMonth(expiresAt.getMonth() + (months || 1))

    const tenant = await prisma.tenant.create({
      data: {
        name,
        slug: slug.toLowerCase().replace(/\s+/g, '-'),
        phone,
        address,
        plan: plan || 'monthly',
        expiresAt,
        status: 'active',
        // Default kategoriyalar
        categories: {
          create: [
            { name: 'Oziq-ovqat',       color: '#4CAF50' },
            { name: 'Ichimliklar',       color: '#2196F3' },
            { name: 'Sut mahsulotlari', color: '#FFF176' },
            { name: 'Non va shirinlik', color: '#FF9800' },
            { name: 'Sabzavotlar',      color: '#8BC34A' },
            { name: 'Mevalar',          color: '#E91E63' },
            { name: 'Uy kimyosi',       color: '#9C27B0' },
            { name: 'Boshqalar',        color: '#607D8B' },
          ]
        },
        // Admin user
        users: {
          create: {
            username: adminUsername,
            passwordHash: await bcrypt.hash(adminPassword, 10),
            fullName: `${name} Admin`,
            role: 'admin'
          }
        }
      },
      include: { users: { select: { id: true, username: true, role: true } } }
    })

    res.status(201).json({
      ...tenant,
      message: `Do'kon muvaffaqiyatli yaratildi! Login: ${adminUsername}`
    })
  } catch (err) {
    res.status(500).json({ error: err.message })
  }
})

// ─── Tenant yangilash (obuna uzaytirish, bloklash) ────────────────────────────
// PATCH /api/super/tenants/:id
router.patch('/tenants/:id', async (req, res) => {
  try {
    const { id } = req.params
    const { status, months, name, phone, address, logoUrl } = req.body

    const tenant = await prisma.tenant.findUnique({ where: { id: +id } })
    if (!tenant) return res.status(404).json({ error: "Do'kon topilmadi" })

    let expiresAt = tenant.expiresAt
    if (months) {
      // Agar muddati tugagan bo'lsa, hozirdan hisoblash
      const base = new Date() > tenant.expiresAt ? new Date() : tenant.expiresAt
      expiresAt = new Date(base)
      expiresAt.setMonth(expiresAt.getMonth() + months)
    }

    const updated = await prisma.tenant.update({
      where: { id: +id },
      data: {
        ...(status && { status }),
        ...(name && { name }),
        ...(phone && { phone }),
        ...(address && { address }),
        ...(logoUrl && { logoUrl }),
        expiresAt,
        ...(months && { status: 'active' }) // Uzaytirsa avtomatik aktiv
      }
    })

    res.json(updated)
  } catch (err) {
    res.status(500).json({ error: err.message })
  }
})

// ─── Tenant o'chirish ─────────────────────────────────────────────────────────
// DELETE /api/super/tenants/:id
router.delete('/tenants/:id', async (req, res) => {
  try {
    await prisma.tenant.delete({ where: { id: +req.params.id } })
    res.json({ message: "Do'kon o'chirildi" })
  } catch (err) {
    res.status(500).json({ error: err.message })
  }
})

// ─── Umumiy statistika ────────────────────────────────────────────────────────
// GET /api/super/stats
router.get('/stats', async (req, res) => {
  try {
    const [totalTenants, activeTenants, expiredTenants, totalSales] = await Promise.all([
      prisma.tenant.count(),
      prisma.tenant.count({ where: { status: 'active' } }),
      prisma.tenant.count({ where: { status: 'expired' } }),
      prisma.sale.aggregate({ _sum: { total: true }, _count: true })
    ])

    // Muddati 7 kun ichida tugaydigan tenantlar
    const soonExpiring = await prisma.tenant.findMany({
      where: {
        status: 'active',
        expiresAt: {
          lte: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000)
        }
      },
      select: { id: true, name: true, expiresAt: true, phone: true }
    })

    res.json({
      totalTenants,
      activeTenants,
      expiredTenants,
      totalRevenue: totalSales._sum.total || 0,
      totalTransactions: totalSales._count,
      soonExpiring
    })
  } catch (err) {
    res.status(500).json({ error: err.message })
  }
})

// ─── Tenant API key yangilash ─────────────────────────────────────────────────
router.post('/tenants/:id/reset-key', async (req, res) => {
  try {
    const { v4: uuidv4 } = await import('uuid')
    const updated = await prisma.tenant.update({
      where: { id: +req.params.id },
      data: { apiKey: uuidv4() }
    })
    res.json({ apiKey: updated.apiKey })
  } catch (err) {
    res.status(500).json({ error: err.message })
  }
})

export default router
