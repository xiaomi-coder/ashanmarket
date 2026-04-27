import { useState, useEffect } from 'react'
import { api } from '../lib/api.js'
import toast from 'react-hot-toast'

const C = { bg:'#0f1117', card:'#1a1d27', border:'#2e3460', accent:'#3d7fff',
            green:'#27ae60', red:'#e74c3c', orange:'#f39c12', text:'#f0f2ff', muted:'#8892b0' }
const fmt = n => Number(n || 0).toLocaleString('uz-UZ')

const EMPTY = { barcode:'', name:'', price:'', costPrice:'', stock:'', lowStockThreshold:'10', categoryId:'', unit:'dona' }

export default function Products() {
  const [products, setProducts]     = useState([])
  const [categories, setCategories] = useState([])
  const [form, setForm]             = useState(EMPTY)
  const [editId, setEditId]         = useState(null)
  const [search, setSearch]         = useState('')
  const [loading, setLoading]       = useState(false)

  useEffect(() => { load() }, [])

  const load = async () => {
    try {
      const [p, c] = await Promise.all([
        api.get('/api/products/web'),
        api.get('/api/products/web/categories')
      ])
      setProducts(p.data)
      setCategories(c.data)
    } catch { toast.error('Yuklashda xatolik') }
  }

  const filtered = products.filter(p =>
    p.name.toLowerCase().includes(search.toLowerCase()) ||
    p.barcode.includes(search)
  )

  const save = async (e) => {
    e.preventDefault()
    setLoading(true)
    try {
      if (editId) {
        await api.put(`/api/products/web/${editId}`, form)
        toast.success('Yangilandi!')
      } else {
        await api.post('/api/products/web', form)
        toast.success("Qo'shildi!")
      }
      setForm(EMPTY); setEditId(null)
      load()
    } catch (err) {
      toast.error(err.response?.data?.error || 'Xatolik')
    } finally { setLoading(false) }
  }

  const edit = (p) => {
    setForm({
      barcode: p.barcode, name: p.name, price: p.price, costPrice: p.costPrice,
      stock: p.stock, lowStockThreshold: p.lowStockThreshold,
      categoryId: p.categoryId || '', unit: p.unit
    })
    setEditId(p.id)
    window.scrollTo(0, 0)
  }

  const del = async (id) => {
    if (!confirm("O'chirishni tasdiqlang?")) return
    try { await api.delete(`/api/products/web/${id}`); load(); toast.success("O'chirildi") }
    catch { toast.error('Xatolik') }
  }

  const inp = (key) => ({
    value: form[key], onChange: e => setForm(p => ({ ...p, [key]: e.target.value })),
    style: { width:'100%', padding:'9px 12px', background:'#0f1117', border:`1px solid ${C.border}`,
              borderRadius:8, color:C.text, fontSize:13, outline:'none' }
  })

  return (
    <div style={{ padding:20, maxWidth:1400, margin:'0 auto' }}>
      <h1 style={{ color:C.text, fontSize:20, fontWeight:800, marginBottom:20 }}>📦 Mahsulotlar</h1>

      <div style={{ display:'grid', gridTemplateColumns:'340px 1fr', gap:16 }}>

        {/* Form */}
        <div style={{ background:C.card, borderRadius:12, padding:20,
                      border:`1px solid ${C.border}`, alignSelf:'start' }}>
          <h2 style={{ color:C.text, fontSize:14, fontWeight:700, marginBottom:16 }}>
            {editId ? '✏️ Tahrirlash' : '➕ Yangi mahsulot'}
          </h2>
          <form onSubmit={save} style={{ display:'flex', flexDirection:'column', gap:12 }}>
            {[
              { key:'barcode', label:'SHTRIX-KOD *' },
              { key:'name',    label:'NOMI *' },
              { key:'price',   label:'SOTUV NARXI *', type:'number' },
              { key:'costPrice', label:'TAN NARXI', type:'number' },
              { key:'stock',   label:'QOLDIQ', type:'number' },
              { key:'lowStockThreshold', label:'KAM SIGNAL', type:'number' },
            ].map(f => (
              <div key={f.key}>
                <label style={{ color:C.muted, fontSize:11, fontWeight:600,
                                letterSpacing:1, display:'block', marginBottom:4 }}>
                  {f.label}
                </label>
                <input type={f.type || 'text'} required={f.label.includes('*')} {...inp(f.key)} />
              </div>
            ))}

            <div>
              <label style={{ color:C.muted, fontSize:11, fontWeight:600, letterSpacing:1,
                              display:'block', marginBottom:4 }}>O'LCHOV</label>
              <select {...inp('unit')} style={{ ...inp('unit').style }}>
                {['dona','kg','litr','paket','quti','shisha','tuba','bog\''].map(u =>
                  <option key={u} value={u}>{u}</option>
                )}
              </select>
            </div>

            <div>
              <label style={{ color:C.muted, fontSize:11, fontWeight:600, letterSpacing:1,
                              display:'block', marginBottom:4 }}>KATEGORIYA</label>
              <select {...inp('categoryId')} style={{ ...inp('categoryId').style }}>
                <option value="">Tanlang...</option>
                {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </div>

            <div style={{ display:'flex', gap:8 }}>
              {editId && (
                <button type="button" onClick={() => { setForm(EMPTY); setEditId(null) }} style={{
                  flex:1, padding:'10px', background:'transparent', border:`1px solid ${C.border}`,
                  borderRadius:8, color:C.muted, cursor:'pointer', fontSize:13
                }}>Bekor</button>
              )}
              <button type="submit" disabled={loading} style={{
                flex:1, padding:'10px', background:C.accent, border:'none',
                borderRadius:8, color:'#fff', fontWeight:700, cursor:'pointer', fontSize:13
              }}>
                {loading ? '...' : editId ? '💾 Yangilash' : '➕ Qo\'shish'}
              </button>
            </div>
          </form>
        </div>

        {/* Table */}
        <div style={{ background:C.card, borderRadius:12, padding:20,
                      border:`1px solid ${C.border}` }}>
          <div style={{ display:'flex', gap:12, marginBottom:16, alignItems:'center' }}>
            <input placeholder="🔍 Qidirish..." value={search}
              onChange={e => setSearch(e.target.value)}
              style={{ flex:1, padding:'9px 14px', background:'#0f1117',
                       border:`1px solid ${C.border}`, borderRadius:8,
                       color:C.text, fontSize:13, outline:'none' }}
            />
            <span style={{ color:C.muted, fontSize:13 }}>{filtered.length} ta</span>
            <button onClick={load} style={{ padding:'9px 16px', background:'#22263a',
              border:`1px solid ${C.border}`, borderRadius:8, color:C.muted,
              fontSize:12, cursor:'pointer' }}>♻️ Yangilash</button>
          </div>

          <div style={{ overflowX:'auto' }}>
            <table style={{ width:'100%', borderCollapse:'collapse', fontSize:13 }}>
              <thead>
                <tr>
                  {['Barcode','Nomi','Narx','Tan narx','Qoldiq','O\'lchov','Kategoriya',''].map(h => (
                    <th key={h} style={{ textAlign:'left', padding:'8px 10px', color:C.muted,
                                         fontSize:11, fontWeight:600, letterSpacing:0.5,
                                         borderBottom:`1px solid ${C.border}` }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {filtered.map(p => (
                  <tr key={p.id} style={{ borderBottom:`1px solid ${C.border}` }}
                    onMouseEnter={e => e.currentTarget.style.background='#22263a'}
                    onMouseLeave={e => e.currentTarget.style.background='transparent'}>
                    <td style={{ padding:'10px', color:C.muted, fontFamily:'monospace' }}>{p.barcode}</td>
                    <td style={{ padding:'10px', color:C.text, fontWeight:500 }}>
                      {p.name}
                      {p.stock <= p.lowStockThreshold && (
                        <span style={{ marginLeft:6, background:C.orange, color:'#fff',
                                       fontSize:10, padding:'1px 6px', borderRadius:4 }}>KAM</span>
                      )}
                    </td>
                    <td style={{ padding:'10px', color:C.accent }}>{fmt(p.price)}</td>
                    <td style={{ padding:'10px', color:C.muted }}>{fmt(p.costPrice)}</td>
                    <td style={{ padding:'10px', color: p.stock === 0 ? C.red :
                                                        p.stock <= p.lowStockThreshold ? C.orange : C.green,
                                 fontWeight:700 }}>{p.stock}</td>
                    <td style={{ padding:'10px', color:C.muted }}>{p.unit}</td>
                    <td style={{ padding:'10px', color:C.muted }}>{p.category?.name || '—'}</td>
                    <td style={{ padding:'10px' }}>
                      <div style={{ display:'flex', gap:4 }}>
                        <button onClick={() => edit(p)} style={{ padding:'5px 10px',
                          background:'#22263a', border:`1px solid ${C.border}`,
                          borderRadius:6, color:C.muted, fontSize:12, cursor:'pointer' }}>✏️</button>
                        <button onClick={() => del(p.id)} style={{ padding:'5px 10px',
                          background:'transparent', border:`1px solid ${C.border}`,
                          borderRadius:6, color:C.red, fontSize:12, cursor:'pointer' }}>🗑</button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  )
}
