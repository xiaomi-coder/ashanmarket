import { useState, useEffect } from 'react'
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer } from 'recharts'
import { api } from '../lib/api.js'
import toast from 'react-hot-toast'

const C = { bg:'#0f1117', card:'#1a1d27', border:'#2e3460', accent:'#3d7fff',
            green:'#27ae60', red:'#e74c3c', orange:'#f39c12', text:'#f0f2ff', muted:'#8892b0' }
const fmt = n => Number(n || 0).toLocaleString('uz-UZ')

const today = () => new Date().toISOString().slice(0,10)
const daysAgo = (n) => new Date(Date.now() - n * 86400000).toISOString().slice(0,10)

export default function Reports() {
  const [from, setFrom]       = useState(today())
  const [to, setTo]           = useState(today())
  const [report, setReport]   = useState(null)
  const [sales, setSales]     = useState([])
  const [loading, setLoading] = useState(false)

  useEffect(() => { load() }, [])

  const load = async () => {
    setLoading(true)
    try {
      const [r, s] = await Promise.all([
        api.get(`/api/sales/web/report?from=${from}&to=${to}`),
        api.get(`/api/sales/web?from=${from}&to=${to}&limit=20`)
      ])
      setReport(r.data); setSales(s.data)
    } catch { toast.error('Xatolik') }
    finally { setLoading(false) }
  }

  const setRange = async (f, t) => {
    setFrom(f); setTo(t)
    setLoading(true)
    try {
      const [r, s] = await Promise.all([
        api.get(`/api/sales/web/report?from=${f}&to=${t}`),
        api.get(`/api/sales/web?from=${f}&to=${t}&limit=20`)
      ])
      setReport(r.data); setSales(s.data)
    } catch { toast.error('Xatolik') }
    finally { setLoading(false) }
  }

  const stats = [
    { label:'Jami sotuvlar', value: report?.totalTransactions || 0, suffix:'ta', color:C.accent },
    { label:'Tushum', value: fmt(report?.totalRevenue), suffix:"so'm", color:C.text },
    { label:'Sof foyda', value: fmt(report?.totalProfit), suffix:"so'm", color:C.green },
    { label:'Chegirmalar', value: fmt(report?.totalDiscount), suffix:"so'm", color:C.orange },
  ]

  return (
    <div style={{ padding:20, maxWidth:1400 }}>
      <h1 style={{ color:C.text, fontSize:20, fontWeight:800, marginBottom:20 }}>📈 Hisobotlar</h1>

      {/* Filters */}
      <div style={{ background:C.card, borderRadius:12, padding:16, border:`1px solid ${C.border}`,
                    display:'flex', gap:12, alignItems:'center', flexWrap:'wrap', marginBottom:16 }}>
        <div style={{ display:'flex', gap:8, alignItems:'center' }}>
          <label style={{ color:C.muted, fontSize:12 }}>DAN:</label>
          <input type="date" value={from} onChange={e => setFrom(e.target.value)}
            style={{ padding:'8px 12px', background:'#0f1117', border:`1px solid ${C.border}`,
                     borderRadius:8, color:C.text, fontSize:13, outline:'none' }}/>
          <label style={{ color:C.muted, fontSize:12 }}>GACHA:</label>
          <input type="date" value={to} onChange={e => setTo(e.target.value)}
            style={{ padding:'8px 12px', background:'#0f1117', border:`1px solid ${C.border}`,
                     borderRadius:8, color:C.text, fontSize:13, outline:'none' }}/>
        </div>
        <button onClick={load} style={{ padding:'8px 20px', background:C.accent, border:'none',
          borderRadius:8, color:'#fff', fontSize:13, fontWeight:600, cursor:'pointer' }}>
          🔍 Ko'rsatish
        </button>
        {[
          { label:'Bugun',   fn: () => setRange(today(), today()) },
          { label:'7 kun',   fn: () => setRange(daysAgo(7), today()) },
          { label:'30 kun',  fn: () => setRange(daysAgo(30), today()) },
        ].map(b => (
          <button key={b.label} onClick={b.fn} style={{
            padding:'8px 16px', background:'#22263a', border:`1px solid ${C.border}`,
            borderRadius:8, color:C.muted, fontSize:12, cursor:'pointer'
          }}>{b.label}</button>
        ))}
        {loading && <span style={{ color:C.muted, fontSize:13 }}>Yuklanmoqda...</span>}
      </div>

      {/* Stats */}
      <div style={{ display:'grid', gridTemplateColumns:'repeat(4,1fr)', gap:12, marginBottom:16 }}>
        {stats.map(s => (
          <div key={s.label} style={{ background:C.card, borderRadius:12, padding:20,
                                      border:`1px solid ${C.border}` }}>
            <div style={{ color:C.muted, fontSize:11, fontWeight:600, letterSpacing:1 }}>
              {s.label.toUpperCase()}
            </div>
            <div style={{ color:s.color, fontSize:28, fontWeight:800, marginTop:6 }}>
              {s.value}
            </div>
            <div style={{ color:C.muted, fontSize:12 }}>{s.suffix}</div>
          </div>
        ))}
      </div>

      <div style={{ display:'grid', gridTemplateColumns:'1fr 320px', gap:16 }}>

        {/* Sales history */}
        <div style={{ background:C.card, borderRadius:12, padding:20, border:`1px solid ${C.border}` }}>
          <h2 style={{ color:C.text, fontSize:14, fontWeight:700, marginBottom:16 }}>Sotuv tarixi</h2>
          <table style={{ width:'100%', borderCollapse:'collapse', fontSize:13 }}>
            <thead>
              <tr>
                {['Chek №','Kassir','Jami','To\'lov','Sana'].map(h => (
                  <th key={h} style={{ textAlign:'left', padding:'8px', color:C.muted,
                                       fontSize:11, borderBottom:`1px solid ${C.border}` }}>{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {sales.map(s => (
                <tr key={s.id} style={{ borderBottom:`1px solid ${C.border}` }}>
                  <td style={{ padding:'10px 8px', color:C.text, fontFamily:'monospace' }}>{s.saleNumber}</td>
                  <td style={{ padding:'10px 8px', color:C.muted }}>{s.cashierName}</td>
                  <td style={{ padding:'10px 8px', color:C.accent, fontWeight:700 }}>
                    {fmt(s.total)} so'm
                  </td>
                  <td style={{ padding:'10px 8px', color:C.muted }}>{s.paymentMethod}</td>
                  <td style={{ padding:'10px 8px', color:C.muted }}>
                    {new Date(s.createdAt).toLocaleString('uz-UZ')}
                  </td>
                </tr>
              ))}
              {sales.length === 0 && (
                <tr><td colSpan={5} style={{ padding:20, textAlign:'center', color:C.muted }}>
                  Ma'lumot yo'q
                </td></tr>
              )}
            </tbody>
          </table>
        </div>

        {/* Top products */}
        <div style={{ background:C.card, borderRadius:12, padding:20, border:`1px solid ${C.border}` }}>
          <h2 style={{ color:C.text, fontSize:14, fontWeight:700, marginBottom:16 }}>
            🏆 Top mahsulotlar
          </h2>
          {(report?.topProducts || []).map((p, i) => (
            <div key={p.productName} style={{ marginBottom:14, paddingBottom:14,
                                              borderBottom:`1px solid ${C.border}` }}>
              <div style={{ display:'flex', justifyContent:'space-between', alignItems:'center' }}>
                <div style={{ display:'flex', gap:8, alignItems:'center' }}>
                  <span style={{ color:C.accent, fontWeight:800, fontSize:14,
                                 minWidth:20 }}>#{i+1}</span>
                  <div>
                    <div style={{ color:C.text, fontSize:13, fontWeight:500 }}>
                      {p.productName}
                    </div>
                    <div style={{ color:C.muted, fontSize:11 }}>{p.quantitySold} ta sotildi</div>
                  </div>
                </div>
                <div style={{ textAlign:'right' }}>
                  <div style={{ color:C.accent, fontWeight:700, fontSize:13 }}>
                    {fmt(p.revenue)} so'm
                  </div>
                  <div style={{ color:C.green, fontSize:11 }}>
                    Foyda: {fmt(p.profit)}
                  </div>
                </div>
              </div>
            </div>
          ))}
          {(!report?.topProducts?.length) && (
            <p style={{ color:C.muted, textAlign:'center', padding:20 }}>Ma'lumot yo'q</p>
          )}
        </div>
      </div>
    </div>
  )
}
