import { Router } from 'express'
import prisma from '../lib/prisma.js'
import { authMiddleware, adminOnly, apiKeyAuth } from '../middleware/auth.js'

const router = Router()

// ─── Web routes ───────────────────────────────────────────────────────────────
router.use('/web', authMiddleware)

// POST /api/sales/web — Yangi sotuv
router.post('/web', async (req, res) => {
  try {
    const { items, discount, total, amountPaid, paymentMethod, subTotal } = req.body

    if (!items?.length) return res.status(400).json({ error: 'Savat bo\'sh' })
    if (amountPaid < total) return res.status(400).json({ error: 'To\'lov yetarli emas' })

    // Sotuv raqami
    const todayCount = await prisma.sale.count({
      where: {
        tenantId: req.tenant.id,
        createdAt: { gte: new Date(new Date().setHours(0,0,0,0)) }
      }
    })
    const saleNumber = `${new Date().toISOString().slice(0,10).replace(/-/g,'')}-${String(todayCount+1).padStart(4,'0')}`

    const sale = await prisma.$transaction(async (tx) => {
      const newSale = await tx.sale.create({
        data: {
          tenantId: req.tenant.id,
          saleNumber,
          userId: req.user.userId,
          cashierName: req.user.fullName,
          subTotal: +subTotal,
          discount: +(discount || 0),
          total: +total,
          amountPaid: +amountPaid,
          change: +(amountPaid - total).toFixed(2),
          paymentMethod: paymentMethod || 'Naqd',
          items: {
            create: items.map(item => ({
              productId: item.productId,
              productName: item.productName,
              barcode: item.barcode || '',
              unitPrice: +item.unitPrice,
              costPrice: +(item.costPrice || 0),
              quantity: +item.quantity,
              discount: +(item.discount || 0)
            }))
          }
        },
        include: { items: true }
      })

      // Stock kamaytirish
      for (const item of items) {
        await tx.product.update({
          where: { id: item.productId },
          data: { stock: { decrement: item.quantity } }
        })
      }

      return newSale
    })

    res.status(201).json(sale)
  } catch (err) {
    res.status(500).json({ error: err.message })
  }
})

// GET /api/sales/web — Sotuv tarixi
router.get('/web', adminOnly, async (req, res) => {
  try {
    const { from, to, page = 1, limit = 50 } = req.query
    const sales = await prisma.sale.findMany({
      where: {
        tenantId: req.tenant.id,
        ...(from && to && {
          createdAt: { gte: new Date(from), lte: new Date(new Date(to).setHours(23,59,59)) }
        })
      },
      include: { items: true },
      orderBy: { createdAt: 'desc' },
      skip: (+page - 1) * +limit,
      take: +limit
    })
    res.json(sales)
  } catch (err) {
    res.status(500).json({ error: err.message })
  }
})

// GET /api/sales/web/report — Hisobot
router.get('/web/report', adminOnly, async (req, res) => {
  try {
    const { from, to } = req.query
    const dateFilter = {
      tenantId: req.tenant.id,
      status: 'completed',
      ...(from && to && {
        createdAt: { gte: new Date(from), lte: new Date(new Date(to).setHours(23,59,59)) }
      })
    }

    const [summary, topProducts] = await Promise.all([
      prisma.sale.aggregate({
        where: dateFilter,
        _sum: { total: true, discount: true, amountPaid: true },
        _count: true
      }),
      prisma.saleItem.groupBy({
        by: ['productId', 'productName', 'barcode'],
        where: { sale: dateFilter },
        _sum: { quantity: true, unitPrice: true, costPrice: true },
        orderBy: { _sum: { quantity: 'desc' } },
        take: 10
      })
    ])

    // Foyda hisoblash
    const profitData = await prisma.saleItem.findMany({
      where: { sale: dateFilter },
      select: { unitPrice: true, costPrice: true, quantity: true }
    })
    const totalCost = profitData.reduce((s, i) => s + (Number(i.costPrice) * i.quantity), 0)
    const totalRevenue = Number(summary._sum.total || 0)

    res.json({
      totalTransactions: summary._count,
      totalRevenue,
      totalCost,
      totalProfit: totalRevenue - totalCost,
      totalDiscount: Number(summary._sum.discount || 0),
      topProducts: topProducts.map(p => ({
        productName: p.productName,
        barcode: p.barcode,
        quantitySold: p._sum.quantity,
        revenue: Number(p._sum.unitPrice) * p._sum.quantity,
        profit: (Number(p._sum.unitPrice) - Number(p._sum.costPrice)) * p._sum.quantity
      }))
    })
  } catch (err) {
    res.status(500).json({ error: err.message })
  }
})

// GET /api/sales/web/dashboard — Dashboard uchun maxsus (oy bo'yicha)
router.get('/web/dashboard', adminOnly, async (req, res) => {
  try {
    const { month } = req.query; // format: YYYY-MM
    let startDate, endDate;

    if (month) {
      const [y, m] = month.split('-');
      startDate = new Date(y, m - 1, 1);
      endDate = new Date(y, m, 0, 23, 59, 59, 999);
    } else {
      const now = new Date();
      startDate = new Date(now.getFullYear(), now.getMonth(), 1);
      endDate = new Date(now.getFullYear(), now.getMonth() + 1, 0, 23, 59, 59, 999);
    }

    // 1. Umumiy statistika
    const periodSales = await prisma.sale.aggregate({
      where: { tenantId: req.tenant.id, status: 'completed', createdAt: { gte: startDate, lte: endDate } },
      _sum: { total: true }, _count: true
    })
    
    const profitData = await prisma.saleItem.findMany({
      where: { sale: { tenantId: req.tenant.id, status: 'completed', createdAt: { gte: startDate, lte: endDate } } },
      select: { unitPrice: true, costPrice: true, quantity: true }
    })
    const totalCost = profitData.reduce((s, i) => s + (Number(i.costPrice) * i.quantity), 0)
    const totalRevenue = Number(periodSales._sum.total || 0)

    // 2. KUNLIK GRAFIK (Chart)
    const dailySalesRaw = await prisma.sale.findMany({
      where: { tenantId: req.tenant.id, status: 'completed', createdAt: { gte: startDate, lte: endDate } },
      select: { createdAt: true, total: true }
    });
    
    const chartMap = {};
    dailySalesRaw.forEach(s => {
      const day = s.createdAt.toISOString().slice(0, 10);
      chartMap[day] = (chartMap[day] || 0) + s.total;
    });
    
    const chartData = Object.keys(chartMap).sort().map(date => ({
      date,
      revenue: chartMap[date]
    }));

    // Smart Insights
    const totalDays = Object.keys(chartMap).length || 1;
    const avgDaily = totalRevenue / totalDays;
    let bestDay = { date: '-', revenue: 0 };
    chartData.forEach(d => { if (d.revenue > bestDay.revenue) bestDay = d });

    // 3. Top and Slow products
    const recentSales = await prisma.saleItem.groupBy({
      by: ['productId', 'productName', 'barcode'],
      where: { sale: { tenantId: req.tenant.id, status: 'completed', createdAt: { gte: startDate, lte: endDate } } },
      _sum: { quantity: true, unitPrice: true }
    })

    const topProducts = [...recentSales]
      .sort((a, b) => b._sum.quantity - a._sum.quantity)
      .slice(0, 10)
      .map(p => ({
        productName: p.productName,
        barcode: p.barcode,
        quantitySold: p._sum.quantity,
        revenue: Number(p._sum.unitPrice) * p._sum.quantity
      }))

    const allProducts = await prisma.product.findMany({
      where: { tenantId: req.tenant.id, isActive: true },
      select: { id: true, name: true, stock: true, barcode: true, price: true }
    })

    const salesMap = {}
    recentSales.forEach(s => salesMap[s.productId] = s._sum.quantity)

    const slowProducts = allProducts.map(p => ({
      productName: p.name,
      barcode: p.barcode,
      stock: p.stock,
      price: p.price,
      quantitySold: salesMap[p.id] || 0
    }))
    .sort((a, b) => a.quantitySold - b.quantitySold)
    .slice(0, 10)

    res.json({
      summary: {
        transactions: periodSales._count,
        revenue: totalRevenue,
        profit: totalRevenue - totalCost,
        avgDaily,
        bestDay
      },
      chartData,
      topProducts,
      slowProducts
    })
  } catch (err) {
    res.status(500).json({ error: err.message })
  }
})

// ─── EXE Sync routes ──────────────────────────────────────────────────────────
router.use('/sync', apiKeyAuth)

// POST /api/sales/sync — EXE dan sotuv yuklash
router.post('/sync', async (req, res) => {
  try {
    const { sales } = req.body // Array of sales from EXE

    const results = []
    for (const s of sales) {
      try {
        const exists = await prisma.sale.findUnique({
          where: { tenantId_saleNumber: { tenantId: req.tenant.id, saleNumber: s.saleNumber } }
        })
        if (exists) { results.push({ saleNumber: s.saleNumber, status: 'skip' }); continue }

        await prisma.$transaction(async (tx) => {
          const sale = await tx.sale.create({
            data: {
              tenantId: req.tenant.id,
              saleNumber: s.saleNumber,
              userId: 1, // default
              cashierName: s.cashierName || 'Kassir',
              subTotal: +s.subTotal,
              discount: +(s.discount || 0),
              total: +s.total,
              amountPaid: +s.amountPaid,
              change: +(s.change || 0),
              paymentMethod: s.paymentMethod || 'Naqd',
              syncedFromExe: true,
              createdAt: new Date(s.createdAt),
              items: {
                create: s.items.map(item => ({
                  productId: item.productId,
                  productName: item.productName,
                  barcode: item.barcode || '',
                  unitPrice: +item.unitPrice,
                  costPrice: +(item.costPrice || 0),
                  quantity: +item.quantity,
                  discount: 0
                }))
              }
            }
          })
          results.push({ saleNumber: s.saleNumber, status: 'ok', id: sale.id })
        })
      } catch (e) {
        results.push({ saleNumber: s.saleNumber, status: 'error', error: e.message })
      }
    }

    res.json({ synced: results.filter(r => r.status === 'ok').length, results })
  } catch (err) {
    res.status(500).json({ error: err.message })
  }
})

export default router
