import { useState, useEffect } from 'react'
import { api } from '../lib/api.js'
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, AreaChart, Area } from 'recharts'

const C = { bg:'#0f1117', card:'#1a1d27', border:'#2e3460', accent:'#3d7fff',
            green:'#27ae60', red:'#e74c3c', orange:'#f39c12', text:'#f0f2ff', muted:'#8892b0' }
const fmt = n => Number(n||0).toLocaleString('uz-UZ')

export default function Dashboard() {
  const [data, setData]         = useState(null)
  const [lowStock, setLowStock] = useState([])
  const [loading, setLoading]   = useState(true)
  const [month, setMonth]       = useState(new Date().toISOString().slice(0, 7)) // YYYY-MM

  useEffect(() => {
    setLoading(true)
    Promise.all([
      api.get(`/api/sales/web/dashboard?month=${month}`),
      api.get('/api/products/web?search=').catch(() => ({ data: [] }))
    ]).then(([dashRes, prodRes]) => {
      setData(dashRes.data)
      setLowStock(prodRes.data.filter(x => x.stock <= x.lowStockThreshold))
      setLoading(false)
    }).catch(err => {
      console.error(err)
      setLoading(false)
    })
  }, [month])

  return (
    <div style={{ padding:24, maxWidth:1200, margin:'0 auto' }}>
      
      {/* Header and Month Picker */}
      <div style={{ display:'flex', justifyContent:'space-between', alignItems:'center', marginBottom:24 }}>
        <div>
          <h1 style={{ color:C.text, fontSize:22, fontWeight:800, marginBottom:4 }}>
            📊 Analitika va Boshqaruv
          </h1>
          <p style={{ color:C.muted, fontSize:13 }}>
            Biznesingizning to'liq holati haqida aqlli hisobot
          </p>
        </div>
        <div style={{ background:C.card, padding:'6px 12px', borderRadius:8, border:`1px solid ${C.border}`, display:'flex', alignItems:'center', gap:10 }}>
          <span style={{ color:C.muted, fontSize:13, fontWeight:600 }}>OYNI TANLASH:</span>
          <input 
            type="month" 
            value={month} 
            onChange={e => setMonth(e.target.value)}
            style={{ 
              background:'transparent', border:'none', color:C.text, 
              fontSize:15, fontWeight:700, outline:'none', cursor:'pointer' 
            }}
          />
        </div>
      </div>

      {loading ? (
        <div style={{ color:C.accent, padding:40, textAlign:'center', fontSize:16, fontWeight:600 }}>
          ⏳ Ma'lumotlar hisoblanmoqda...
        </div>
      ) : (
        <>
          {/* Top Smart Insights */}
          <div style={{ display:'grid', gridTemplateColumns:'repeat(4,1fr)', gap:12, marginBottom:24 }}>
            {[
              { label:"UMUMIY TUSHUM",   value:fmt(data?.summary?.revenue),    suffix:"so'm", color:C.text, icon:'💰' },
              { label:"SOF FOYDA",       value:fmt(data?.summary?.profit),     suffix:"so'm", color:C.green, icon:'📈' },
              { label:"O'RTACHA KUNLIK", value:fmt(data?.summary?.avgDaily),   suffix:"so'm", color:C.accent, icon:'📅' },
              { label:"ENG ZO'R KUN",    value:data?.summary?.bestDay?.date,   suffix:`(${fmt(data?.summary?.bestDay?.revenue)} so'm)`, color:C.orange, icon:'👑' },
            ].map(s => (
              <div key={s.label} style={{ background:C.card, borderRadius:12, padding:20, border:`1px solid ${C.border}` }}>
                <div style={{ fontSize:28, marginBottom:8 }}>{s.icon}</div>
                <div style={{ color:C.muted, fontSize:11, fontWeight:600, letterSpacing:0.5 }}>
                  {s.label}
                </div>
                <div style={{ color:s.color, fontSize: s.label==="ENG ZO'R KUN" ? 20 : 26, fontWeight:800, marginTop:4 }}>
                  {s.value}
                </div>
                <div style={{ color:C.muted, fontSize:12 }}>{s.suffix}</div>
              </div>
            ))}
          </div>

          {/* Main Chart */}
          <div style={{ background:C.card, borderRadius:12, padding:24, border:`1px solid ${C.border}`, marginBottom:24 }}>
            <h2 style={{ color:C.text, fontSize:15, fontWeight:700, marginBottom:20 }}>
              📈 Kunlik daromad dinamikasi ({month})
            </h2>
            <div style={{ width: '100%', height: 300 }}>
              <ResponsiveContainer>
                <AreaChart data={data?.chartData || []} margin={{ top: 10, right: 10, left: 0, bottom: 0 }}>
                  <defs>
                    <linearGradient id="colorRevenue" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor={C.accent} stopOpacity={0.3}/>
                      <stop offset="95%" stopColor={C.accent} stopOpacity={0}/>
                    </linearGradient>
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" stroke={C.border} vertical={false} />
                  <XAxis dataKey="date" stroke={C.muted} fontSize={11} tickMargin={10} tickFormatter={(val) => val.slice(-2)} />
                  <YAxis stroke={C.muted} fontSize={11} tickFormatter={(val) => (val/1000000).toFixed(1) + 'M'} />
                  <Tooltip 
                    contentStyle={{ background:C.bg, border:`1px solid ${C.border}`, borderRadius:8, color:C.text }}
                    formatter={(value) => [fmt(value) + " so'm", "Daromad"]}
                    labelStyle={{ color:C.muted, marginBottom:4 }}
                  />
                  <Area type="monotone" dataKey="revenue" stroke={C.accent} strokeWidth={3} fillOpacity={1} fill="url(#colorRevenue)" />
                </AreaChart>
              </ResponsiveContainer>
            </div>
          </div>

          <div style={{ display:'grid', gridTemplateColumns:'1fr 1fr 1fr', gap:16 }}>

            {/* Top Products */}
            <div style={{ background:C.card, borderRadius:12, padding:20, border:`1px solid ${C.border}` }}>
              <h2 style={{ color:C.text, fontSize:14, fontWeight:700, marginBottom:16 }}>
                🏆 Oyning eng zo'r mahsulotlari
              </h2>
              {(data?.topProducts||[]).slice(0,8).map((p, i) => (
                <div key={p.productName} style={{ display:'flex', justifyContent:'space-between',
                                                  padding:'10px 0', borderBottom:`1px solid ${C.border}` }}>
                  <div style={{ display:'flex', gap:10, alignItems:'center' }}>
                    <span style={{ color:C.accent, fontWeight:800, minWidth:24 }}>#{i+1}</span>
                    <div>
                      <div style={{ color:C.text, fontSize:13 }}>{p.productName}</div>
                      <div style={{ color:C.muted, fontSize:11 }}>{p.quantitySold} ta sotilgan</div>
                    </div>
                  </div>
                  <div style={{ color:C.green, fontWeight:700, fontSize:13 }}>
                    {fmt(p.revenue)}
                  </div>
                </div>
              ))}
              {!data?.topProducts?.length && (
                <p style={{ color:C.muted, textAlign:'center', padding:20 }}>Bu oyda savdo yo'q</p>
              )}
            </div>

            {/* Slow moving products */}
            <div style={{ background:C.card, borderRadius:12, padding:20, border:`1px solid ${C.border}` }}>
              <h2 style={{ color:C.text, fontSize:14, fontWeight:700, marginBottom:16 }}>
                🐌 Eng kam ketayotgan tovarlar
              </h2>
              <p style={{ fontSize:11, color:C.muted, marginBottom:12 }}>Pulni "muzlatib qo'ygan" mahsulotlar</p>
              {(data?.slowProducts||[]).map((p, i) => (
                <div key={p.productName} style={{ display:'flex', justifyContent:'space-between',
                                                  padding:'10px 0', borderBottom:`1px solid ${C.border}` }}>
                  <div style={{ display:'flex', gap:10, alignItems:'center' }}>
                    <span style={{ color:C.red, fontWeight:800, minWidth:24 }}>{i+1}.</span>
                    <div>
                      <div style={{ color:C.text, fontSize:13 }}>{p.productName}</div>
                      <div style={{ fontSize:11, color: p.quantitySold===0?C.red:C.muted }}>
                        {p.quantitySold === 0 ? "Umuman sotilmagan!" : `${p.quantitySold} ta sotilgan`}
                      </div>
                    </div>
                  </div>
                  <div style={{ textAlign:'right' }}>
                    <div style={{ color:C.orange, fontWeight:700, fontSize:13 }}>{p.stock} ta qoldiq</div>
                  </div>
                </div>
              ))}
              {!data?.slowProducts?.length && (
                <p style={{ color:C.green, textAlign:'center', padding:20 }}>Muammoli tovarlar yo'q</p>
              )}
            </div>

            {/* Low stock alert */}
            <div style={{ background:C.card, borderRadius:12, padding:20, border:`1px solid ${C.border}` }}>
              <h2 style={{ color:C.text, fontSize:14, fontWeight:700, marginBottom:16 }}>
                ⚠️ Kam qolgan mahsulotlar (Kirim qilish kerak)
              </h2>
              {lowStock.slice(0,8).map(p => (
                <div key={p.id} style={{ display:'flex', justifyContent:'space-between',
                                         padding:'10px 0', borderBottom:`1px solid ${C.border}` }}>
                  <div style={{ color:C.text, fontSize:13 }}>{p.name}</div>
                  <span style={{
                    background: p.stock === 0 ? C.red : C.orange,
                    color:'#fff', fontSize:11, padding:'3px 8px', borderRadius:4, fontWeight:600
                  }}>
                    {p.stock === 0 ? 'Tugagan!' : `${p.stock} ${p.unit}`}
                  </span>
                </div>
              ))}
              {lowStock.length === 0 && (
                <p style={{ color:C.green, textAlign:'center', padding:20 }}>
                  ✅ Barcha mahsulotlar yetarli
                </p>
              )}
            </div>

          </div>
        </>
      )}
    </div>
  )
}
