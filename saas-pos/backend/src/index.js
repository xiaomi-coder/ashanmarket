import express from 'express'
import cors from 'cors'
import dotenv from 'dotenv'
import authRoutes      from './routes/auth.js'
import superAdminRoutes from './routes/superadmin.js'
import productRoutes   from './routes/products.js'
import salesRoutes     from './routes/sales.js'

dotenv.config()

const app = express()
const PORT = process.env.PORT || 3000

// ─── Middleware ───────────────────────────────────────────────────────────────
app.use(cors({
  origin: [
    process.env.FRONTEND_URL || 'http://localhost:5173',
    'http://localhost:3001',
    /\.railway\.app$/,
    /\.vercel\.app$/
  ],
  credentials: true
}))
app.use(express.json({ limit: '10mb' }))
app.use(express.urlencoded({ extended: true }))

// ─── Routes ───────────────────────────────────────────────────────────────────
app.use('/api/auth',     authRoutes)
app.use('/api/super',    superAdminRoutes)
app.use('/api/products', productRoutes)
app.use('/api/sales',    salesRoutes)

// ─── Health check ─────────────────────────────────────────────────────────────
app.get('/health', (req, res) => res.json({ status: 'ok', time: new Date() }))

// ─── 404 ──────────────────────────────────────────────────────────────────────
app.use((req, res) => res.status(404).json({ error: 'Route topilmadi' }))

// ─── Error handler ────────────────────────────────────────────────────────────
app.use((err, req, res, next) => {
  console.error(err.stack)
  res.status(500).json({ error: 'Server xatosi' })
})

app.listen(PORT, () => {
  console.log(`✅ Server ishga tushdi: http://localhost:${PORT}`)
  console.log(`📊 Super Admin: POST /api/auth/super-login`)
})
