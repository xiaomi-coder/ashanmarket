import { useState, useEffect, useRef, useCallback } from 'react'
import { api } from '../lib/api.js'
import toast from 'react-hot-toast'

const fmt = n => Number(n || 0).toLocaleString('uz-UZ')

export default function Cashier() {
  const [cart, setCart]           = useState([])
  const [barcode, setBarcode]     = useState('')
  const [search, setSearch]       = useState('')
  const [results, setResults]     = useState([])
  const [discount, setDiscount]   = useState(0)
  const [paid, setPaid]           = useState('')
  const [payMethod, setPayMethod] = useState('Naqd')
  const [loading, setLoading]     = useState(false)
  const [receipt, setReceipt]     = useState(null)
  const barcodeRef = useRef()

  const tenant = JSON.parse(localStorage.getItem('tenant') || '{}')
  const user   = JSON.parse(localStorage.getItem('user') || '{}')

  const subTotal = cart.reduce((s, i) => s + i.price * i.qty, 0)
  const total    = Math.max(0, subTotal - Number(discount || 0))
  const change   = Number(paid || 0) - total

  // Auto-focus barcode on load
  useEffect(() => { barcodeRef.current?.focus() }, [])

  // Keyboard shortcut F5
  useEffect(() => {
    const handler = (e) => { if (e.key === 'F5') { e.preventDefault(); completeSale() } }
    window.addEventListener('keydown', handler)
    return () => window.removeEventListener('keydown', handler)
  }, [cart, total, paid])

  const scanBarcode = async () => {
    if (!barcode.trim()) return
    try {
      const { data } = await api.get(`/api/products/web/barcode/${barcode.trim()}`)
      addToCart(data)
      setBarcode('')
    } catch {
      toast.error(`"${barcode}" topilmadi!`)
      setBarcode('')
    }
    barcodeRef.current?.focus()
  }

  const searchProducts = useCallback(async (q) => {
    if (q.length < 2) { setResults([]); return }
    try {
      const { data } = await api.get(`/api/products/web?search=${q}`)
      setResults(data.slice(0, 8))
    } catch {}
  }, [])

  useEffect(() => {
    const t = setTimeout(() => searchProducts(search), 250)
    return () => clearTimeout(t)
  }, [search])

  const addToCart = (product) => {
    setCart(prev => {
      const ex = prev.find(i => i.id === product.id)
      if (ex) return prev.map(i => i.id === product.id ? { ...i, qty: i.qty + 1 } : i)
      return [...prev, { ...product, qty: 1, price: Number(product.price) }]
    })
    setResults([])
    setSearch('')
  }

  const updateQty = (id, delta) => {
    setCart(prev => prev.map(i => i.id === id
      ? { ...i, qty: Math.max(1, i.qty + delta) } : i))
  }

  const removeItem = (id) => setCart(prev => prev.filter(i => i.id !== id))

  const completeSale = async () => {
    if (!cart.length) return toast.error("Savat bo'sh!")
    if (Number(paid) < total) return toast.error("To'lov yetarli emas!")
    setLoading(true)
    try {
      const { data } = await api.post('/api/sales/web', {
        items: cart.map(i => ({
          productId: i.id, productName: i.name, barcode: i.barcode,
          unitPrice: i.price, costPrice: i.costPrice || 0, quantity: i.qty
        })),
        subTotal, discount: Number(discount || 0), total,
        amountPaid: Number(paid), paymentMethod: payMethod
      })
      setReceipt({ ...data, tenantName: tenant.name, cashierName: user.fullName,
                   change, amountPaid: Number(paid) })
      setCart([]); setDiscount(0); setPaid('')
      toast.success(`Sotuv #${data.saleNumber} yakunlandi! 🎉`)
    } catch (err) {
      toast.error(err.response?.data?.error || 'Xatolik')
    } finally {
      setLoading(false)
    }
  }

  const C = { // colors
    bg: '#0f1117', card: '#1a1d27', border: '#2e3460',
    accent: '#3d7fff', green: '#27ae60', red: '#e74c3c',
    orange: '#f39c12', text: '#f0f2ff', muted: '#8892b0'
  }

  return (
    <div style={{ display:'flex', height:'100vh', background:C.bg, gap:0 }}>

      {/* LEFT */}
      <div style={{ flex:1, padding:20, display:'flex', flexDirection:'column', gap:12, overflow:'auto' }}>

        {/* Barcode input */}
        <div style={{ background:C.card, borderRadius:12, padding:16, border:`1px solid ${C.border}` }}>
          <label style={{ color:C.muted, fontSize:11, fontWeight:600, letterSpacing:1 }}>
            SHTRIX-KOD
          </label>
          <div style={{ display:'flex', gap:8, marginTop:8 }}>
            <input ref={barcodeRef} value={barcode}
              onChange={e => setBarcode(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && scanBarcode()}
              placeholder="Shtrix-kodni o'qiting yoki kiriting..."
              style={{ flex:1, padding:'12px 16px', background:'#0f1117', border:`1px solid ${C.border}`,
                       borderRadius:8, color:C.text, fontSize:18, fontWeight:700, outline:'none' }}
            />
            <button onClick={scanBarcode} style={{
              padding:'12px 20px', background:C.accent, border:'none',
              borderRadius:8, color:'#fff', fontSize:14, fontWeight:600, cursor:'pointer'
            }}>➕ QO'SHISH</button>
          </div>
        </div>

        {/* Search */}
        <div style={{ background:C.card, borderRadius:12, padding:16, border:`1px solid ${C.border}` }}>
          <input value={search} onChange={e => setSearch(e.target.value)}
            placeholder="🔍 Mahsulot nomini qidirish..."
            style={{ width:'100%', padding:'10px 14px', background:'#0f1117',
                     border:`1px solid ${C.border}`, borderRadius:8, color:C.text,
                     fontSize:13, outline:'none' }}
          />
          {results.length > 0 && (
            <div style={{ marginTop:8 }}>
              {results.map(p => (
                <div key={p.id} onClick={() => addToCart(p)} style={{
                  display:'flex', justifyContent:'space-between', alignItems:'center',
                  padding:'10px 12px', borderRadius:8, cursor:'pointer',
                  borderBottom:`1px solid ${C.border}`, transition:'background 0.1s'
                }}
                  onMouseEnter={e => e.currentTarget.style.background = '#22263a'}
                  onMouseLeave={e => e.currentTarget.style.background = 'transparent'}
                >
                  <div>
                    <div style={{ color:C.text, fontSize:13, fontWeight:500 }}>{p.name}</div>
                    <div style={{ color:C.muted, fontSize:11 }}>{p.barcode} • Qoldiq: {p.stock}</div>
                  </div>
                  <div style={{ color:C.accent, fontWeight:700, fontSize:14 }}>
                    {fmt(p.price)} so'm
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Cart */}
        <div style={{ background:C.card, borderRadius:12, padding:16,
                      border:`1px solid ${C.border}`, flex:1 }}>
          <div style={{ display:'flex', justifyContent:'space-between', marginBottom:12 }}>
            <span style={{ color:C.text, fontWeight:700, fontSize:14 }}>
              🛒 SAVAT ({cart.length} ta mahsulot)
            </span>
            {cart.length > 0 && (
              <button onClick={() => setCart([])} style={{
                background:C.red, border:'none', borderRadius:6,
                color:'#fff', padding:'4px 12px', fontSize:12, cursor:'pointer'
              }}>🗑 Tozalash</button>
            )}
          </div>

          {cart.length === 0 ? (
            <div style={{ textAlign:'center', color:C.muted, padding:40, fontSize:14 }}>
              Savat bo'sh — barcode o'qiting yoki qidiring
            </div>
          ) : (
            <div style={{ display:'flex', flexDirection:'column', gap:6 }}>
              {cart.map(item => (
                <div key={item.id} style={{
                  display:'flex', alignItems:'center', gap:12, padding:'10px 12px',
                  background:'#0f1117', borderRadius:8, border:`1px solid ${C.border}`
                }}>
                  <div style={{ flex:1 }}>
                    <div style={{ color:C.text, fontSize:13, fontWeight:500 }}>{item.name}</div>
                    <div style={{ color:C.muted, fontSize:11 }}>{fmt(item.price)} so'm × {item.qty}</div>
                  </div>
                  <div style={{ display:'flex', alignItems:'center', gap:6 }}>
                    <button onClick={() => updateQty(item.id, -1)} style={{
                      width:28, height:28, background:'#22263a', border:`1px solid ${C.border}`,
                      borderRadius:6, color:C.text, fontSize:16, cursor:'pointer'
                    }}>−</button>
                    <span style={{ color:C.text, fontWeight:700, minWidth:28, textAlign:'center' }}>
                      {item.qty}
                    </span>
                    <button onClick={() => updateQty(item.id, 1)} style={{
                      width:28, height:28, background:C.accent, border:'none',
                      borderRadius:6, color:'#fff', fontSize:14, cursor:'pointer'
                    }}>+</button>
                  </div>
                  <div style={{ minWidth:90, textAlign:'right', color:C.accent,
                                fontWeight:700, fontSize:14 }}>
                    {fmt(item.price * item.qty)}
                  </div>
                  <button onClick={() => removeItem(item.id)} style={{
                    width:28, height:28, background:C.red, border:'none',
                    borderRadius:6, color:'#fff', fontSize:12, cursor:'pointer'
                  }}>✕</button>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* RIGHT - Payment */}
      <div style={{ width:340, background:C.card, borderLeft:`1px solid ${C.border}`,
                    padding:20, display:'flex', flexDirection:'column', gap:16 }}>
        <h2 style={{ color:C.text, fontSize:15, fontWeight:700 }}>💳 TO'LOV</h2>

        {/* Totals */}
        <div style={{ background:'#0f1117', borderRadius:10, padding:16 }}>
          {[
            { label:'Jami:', value: fmt(subTotal) + " so'm", color: C.text },
            { label:'Chegirma:', value: '−' + fmt(discount) + " so'm", color: C.orange },
          ].map(r => (
            <div key={r.label} style={{ display:'flex', justifyContent:'space-between',
                                        marginBottom:8, fontSize:13 }}>
              <span style={{ color:C.muted }}>{r.label}</span>
              <span style={{ color:r.color, fontWeight:500 }}>{r.value}</span>
            </div>
          ))}
          <div style={{ borderTop:`1px solid ${C.border}`, paddingTop:10, marginTop:4,
                        display:'flex', justifyContent:'space-between', alignItems:'center' }}>
            <span style={{ color:C.text, fontWeight:700, fontSize:14 }}>JAMI TO'LOV:</span>
            <span style={{ color:C.accent, fontWeight:800, fontSize:24 }}>
              {fmt(total)} so'm
            </span>
          </div>
        </div>

        {/* Discount */}
        <div>
          <label style={{ color:C.muted, fontSize:11, fontWeight:600, letterSpacing:1 }}>
            CHEGIRMA (so'm)
          </label>
          <input type="number" value={discount} onChange={e => setDiscount(e.target.value)}
            style={{ width:'100%', marginTop:6, padding:'10px 14px', background:'#0f1117',
                     border:`1px solid ${C.border}`, borderRadius:8, color:C.text,
                     fontSize:14, outline:'none' }}
          />
        </div>

        {/* Payment method */}
        <div>
          <label style={{ color:C.muted, fontSize:11, fontWeight:600, letterSpacing:1 }}>
            TO'LOV TURI
          </label>
          <div style={{ display:'flex', gap:6, marginTop:6 }}>
            {['Naqd', 'Karta', 'Transfer'].map(m => (
              <button key={m} onClick={() => setPayMethod(m)} style={{
                flex:1, padding:'8px 4px', borderRadius:8, border:'none', fontSize:12,
                fontWeight:600, cursor:'pointer',
                background: payMethod === m ? C.accent : '#0f1117',
                color: payMethod === m ? '#fff' : C.muted
              }}>{m}</button>
            ))}
          </div>
        </div>

        {/* Amount paid */}
        <div>
          <label style={{ color:C.muted, fontSize:11, fontWeight:600, letterSpacing:1 }}>
            QABUL QILINDI (so'm)
          </label>
          <div style={{ display:'flex', gap:6, marginTop:6 }}>
            <input type="number" value={paid} onChange={e => setPaid(e.target.value)}
              style={{ flex:1, padding:'10px 14px', background:'#0f1117',
                       border:`1px solid ${C.border}`, borderRadius:8, color:C.text,
                       fontSize:18, fontWeight:700, outline:'none' }}
            />
            <button onClick={() => setPaid(String(total))} style={{
              padding:'10px 12px', background:'#22263a', border:`1px solid ${C.border}`,
              borderRadius:8, color:C.muted, fontSize:12, cursor:'pointer'
            }}>= Aniq</button>
          </div>
        </div>

        {/* Quick amounts */}
        <div style={{ display:'flex', flexWrap:'wrap', gap:6 }}>
          {[10000, 20000, 50000, 100000].map(a => (
            <button key={a} onClick={() => setPaid(String(a))} style={{
              flex:'1 0 calc(50% - 3px)', padding:'8px', background:'#0f1117',
              border:`1px solid ${C.border}`, borderRadius:8, color:C.muted,
              fontSize:12, cursor:'pointer'
            }}>{fmt(a)}</button>
          ))}
        </div>

        {/* Change */}
        <div style={{ background:'#0f1117', borderRadius:10, padding:14,
                      display:'flex', justifyContent:'space-between', alignItems:'center' }}>
          <span style={{ color:C.muted, fontWeight:600, fontSize:13 }}>QAYTIM:</span>
          <span style={{ color: change >= 0 ? C.green : C.red, fontWeight:800, fontSize:22 }}>
            {fmt(Math.max(0, change))} so'm
          </span>
        </div>

        {/* Complete */}
        <button onClick={completeSale} disabled={loading || !cart.length || Number(paid) < total}
          style={{
            padding:'16px', background: C.green, border:'none', borderRadius:12,
            color:'#fff', fontSize:16, fontWeight:800, cursor:'pointer',
            opacity: (!cart.length || Number(paid) < total) ? 0.5 : 1, marginTop:'auto'
          }}>
          {loading ? 'Saqlanmoqda...' : '✅ SOTUVNI YAKUNLASH (F5)'}
        </button>
      </div>

      {/* Receipt Modal */}
      {receipt && (
        <div style={{ position:'fixed', inset:0, background:'rgba(0,0,0,0.8)',
                      display:'flex', alignItems:'center', justifyContent:'center', zIndex:100 }}>
          <div style={{ background:C.card, borderRadius:16, padding:28, width:420,
                        border:`1px solid ${C.border}`, maxHeight:'80vh', overflow:'auto' }}>
            <div style={{ display:'flex', justifyContent:'space-between', marginBottom:16 }}>
              <h3 style={{ color:C.text, fontSize:16, fontWeight:700 }}>🧾 CHEK</h3>
              <button onClick={() => setReceipt(null)} style={{
                background:'transparent', border:'none', color:C.muted,
                fontSize:20, cursor:'pointer'
              }}>✕</button>
            </div>
            <pre style={{ fontFamily:'Consolas, monospace', fontSize:12, color:C.text,
                          background:'#0f1117', padding:16, borderRadius:8, whiteSpace:'pre-wrap' }}>
{`═══════════════════════════
  ${receipt.tenantName || 'SUPERMARKET POS'}
═══════════════════════════
Chek №: ${receipt.saleNumber}
Kassir: ${receipt.cashierName}
To'lov: ${receipt.paymentMethod}
───────────────────────────
${receipt.items.map(i =>
  `${i.productName.slice(0,20).padEnd(20)} ${String(i.quantity).padStart(3)} × ${fmt(i.unitPrice).padStart(8)}`
).join('\n')}
═══════════════════════════
JAMI:       ${fmt(receipt.total).padStart(10)} so'm
To'landi:   ${fmt(receipt.amountPaid).padStart(10)} so'm
Qaytim:     ${fmt(receipt.change).padStart(10)} so'm
═══════════════════════════
    Xaridingiz uchun rahmat!`}
            </pre>
            <button onClick={() => { window.print(); setReceipt(null) }} style={{
              width:'100%', marginTop:12, padding:'12px', background:C.accent,
              border:'none', borderRadius:8, color:'#fff', fontSize:14,
              fontWeight:600, cursor:'pointer'
            }}>🖨️ Chek chiqarish</button>
          </div>
        </div>
      )}
    </div>
  )
}
