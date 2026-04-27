import jwt from 'jsonwebtoken'
import prisma from '../lib/prisma.js'

// ─── Tenant user auth ─────────────────────────────────────────────────────────
export const authMiddleware = async (req, res, next) => {
  try {
    const token = req.headers.authorization?.replace('Bearer ', '')
    if (!token) return res.status(401).json({ error: 'Token kerak' })

    const decoded = jwt.verify(token, process.env.JWT_SECRET)
    
    // Tenant obuna tekshirish
    const tenant = await prisma.tenant.findUnique({
      where: { id: decoded.tenantId }
    })
    
    if (!tenant) return res.status(401).json({ error: "Do'kon topilmadi" })
    if (tenant.status === 'blocked') return res.status(403).json({ error: "Do'kon bloklangan" })
    if (tenant.status === 'expired' || new Date() > tenant.expiresAt) {
      await prisma.tenant.update({ where: { id: tenant.id }, data: { status: 'expired' } })
      return res.status(403).json({ error: "Obuna muddati tugagan. Iltimos yangilang!" })
    }

    req.user = decoded
    req.tenant = tenant
    next()
  } catch (err) {
    return res.status(401).json({ error: 'Token yaroqsiz' })
  }
}

// ─── Admin only ───────────────────────────────────────────────────────────────
export const adminOnly = (req, res, next) => {
  if (req.user?.role !== 'admin') {
    return res.status(403).json({ error: "Faqat admin uchun" })
  }
  next()
}

// ─── Super admin auth ─────────────────────────────────────────────────────────
export const superAdminAuth = (req, res, next) => {
  try {
    const token = req.headers.authorization?.replace('Bearer ', '')
    if (!token) return res.status(401).json({ error: 'Token kerak' })

    const decoded = jwt.verify(token, process.env.JWT_SECRET)
    if (decoded.role !== 'superadmin') {
      return res.status(403).json({ error: 'Ruxsat yo\'q' })
    }
    req.superAdmin = decoded
    next()
  } catch {
    return res.status(401).json({ error: 'Token yaroqsiz' })
  }
}

// ─── EXE API Key auth ─────────────────────────────────────────────────────────
export const apiKeyAuth = async (req, res, next) => {
  try {
    const apiKey = req.headers['x-api-key']
    if (!apiKey) return res.status(401).json({ error: 'API key kerak' })

    const tenant = await prisma.tenant.findUnique({ where: { apiKey } })
    if (!tenant) return res.status(401).json({ error: 'API key yaroqsiz' })
    if (tenant.status !== 'active' || new Date() > tenant.expiresAt) {
      return res.status(403).json({ error: 'Obuna faol emas' })
    }

    req.tenant = tenant
    next()
  } catch {
    return res.status(500).json({ error: 'Server xatosi' })
  }
}
