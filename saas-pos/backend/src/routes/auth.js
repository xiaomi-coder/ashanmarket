import { Router } from 'express'
import bcrypt from 'bcryptjs'
import jwt from 'jsonwebtoken'
import prisma from '../lib/prisma.js'

const router = Router()

// ─── Tenant Login ─────────────────────────────────────────────────────────────
// POST /api/auth/login
router.post('/login', async (req, res) => {
  try {
    const { username, password, slug } = req.body

    if (!username || !password || !slug) {
      return res.status(400).json({ error: "Username, parol va do'kon slug kiritilishi kerak" })
    }

    const tenant = await prisma.tenant.findUnique({ where: { slug } })
    if (!tenant) return res.status(404).json({ error: "Do'kon topilmadi" })

    if (tenant.status === 'blocked') {
      return res.status(403).json({ error: "Do'kon bloklangan. Administrator bilan bog'laning" })
    }

    if (new Date() > tenant.expiresAt) {
      await prisma.tenant.update({ where: { id: tenant.id }, data: { status: 'expired' } })
      return res.status(403).json({ error: "Obuna muddati tugagan!" })
    }

    const user = await prisma.user.findUnique({
      where: { tenantId_username: { tenantId: tenant.id, username } }
    })

    if (!user || !user.isActive) {
      return res.status(401).json({ error: "Foydalanuvchi topilmadi yoki faol emas" })
    }

    const valid = await bcrypt.compare(password, user.passwordHash)
    if (!valid) return res.status(401).json({ error: "Parol noto'g'ri" })

    await prisma.user.update({
      where: { id: user.id },
      data: { lastLogin: new Date() }
    })

    const token = jwt.sign(
      { userId: user.id, tenantId: tenant.id, role: user.role, fullName: user.fullName },
      process.env.JWT_SECRET,
      { expiresIn: '12h' }
    )

    res.json({
      token,
      user: { id: user.id, username: user.username, fullName: user.fullName, role: user.role },
      tenant: {
        id: tenant.id,
        name: tenant.name,
        slug: tenant.slug,
        logoUrl: tenant.logoUrl,
        expiresAt: tenant.expiresAt
      }
    })
  } catch (err) {
    res.status(500).json({ error: err.message })
  }
})

// ─── Super Admin Login ────────────────────────────────────────────────────────
// POST /api/auth/super-login
router.post('/super-login', async (req, res) => {
  try {
    const { username, password } = req.body

    const admin = await prisma.superAdmin.findUnique({ where: { username } })
    if (!admin) return res.status(401).json({ error: "Noto'g'ri ma'lumotlar" })

    const valid = await bcrypt.compare(password, admin.passwordHash)
    if (!valid) return res.status(401).json({ error: "Noto'g'ri ma'lumotlar" })

    const token = jwt.sign(
      { adminId: admin.id, username: admin.username, role: 'superadmin' },
      process.env.JWT_SECRET,
      { expiresIn: '24h' }
    )

    res.json({ token, admin: { id: admin.id, username: admin.username } })
  } catch (err) {
    res.status(500).json({ error: err.message })
  }
})

// ─── Me ───────────────────────────────────────────────────────────────────────
router.get('/me', async (req, res) => {
  try {
    const token = req.headers.authorization?.replace('Bearer ', '')
    if (!token) return res.status(401).json({ error: 'Token kerak' })

    const decoded = jwt.verify(token, process.env.JWT_SECRET)
    res.json(decoded)
  } catch {
    res.status(401).json({ error: 'Token yaroqsiz' })
  }
})

export default router
