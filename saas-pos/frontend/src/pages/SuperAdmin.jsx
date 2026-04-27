import { useState, useEffect } from 'react'
import { superApi } from '../lib/api.js'
import toast from 'react-hot-toast'

const C = { bg:'#0f1117', card:'#1a1d27', border:'#2e3460', accent:'#3d7fff',
            green:'#27ae60', red:'#e74c3c', orange:'#f39c12', text:'#f0f2ff', muted:'#8892b0' }
const fmt = n => Number(n||0).toLocaleString('uz-UZ')

const EMPTY_FORM = { name:'', slug:'', phone:'', address:'', plan:'monthly',
                     months:1, adminUsername:'admin', adminPassword:'' }

export default function SuperAdmin() {
  const [tenants, setTenants]   = useState([])
  const [stats, setStats]       = useState(null)
  const [form, setForm]         = useState(EMPTY_FORM)
  const [showForm, setShowForm] = useState(false)
  const [loading, setLoading]   = useState(false)
  const [extendId, setExtendId] = useState(null)
  const [extMonths, setExtMonths] = useState(1)

  const logout = () => { localStorage.removeItem('superToken'); window.location.href='/super-login' }

  useEffect(() => { load() }, [])

  const load = async () => {
    try {
      const [t, s] = await Promise.all([
        superApi.get('/api/super/tenants'),
        superApi.get('/api/super/stats')
      ])
      setTenants(t.data); setStats(s.data)
    } catch { toast.error('Yuklashda xatolik') }
  }

  const create = async (e) => {
    e.preventDefault(); setLoading(true)
    try {
      const { data } = await superApi.post('/api/super/tenants', form)
      toast.success(`✅ ${data.name} yaratildi! API Key: ${data.apiKey?.slice(0,12)}...`)
      setForm(EMPTY_FORM); setShowForm(false); load()
    } catch (err) { toast.error(err.response?.data?.error || 'Xatolik') }
    finally { setLoading(false) }
  }

  const setStatus = async (id, status) => {
    try {
      await superApi.patch(`/api/super/tenants/${id}`, { status })
      toast.success(status === 'blocked' ? 'Bloklandi' : 'Aktivlashtirildi')
      load()
    } catch { toast.error('Xatolik') }
  }

  const extend = async (id) => {
    try {
      await superApi.patch(`/api/super/tenants/${id}`, { months: +extMonths })
      toast.success(`${extMonths} oyga uzaytirildi!`)
      setExtendId(null); load()
    } catch { toast.error('Xatolik') }
  }

  const inp = (key) => ({
    value: form[key], onChange: e => setForm(p => ({ ...p, [key]: e.target.value })),
    style: { width:'100%', padding:'9px 12px', background:'#0f1117',
             border:`1px solid ${C.border}`, borderRadius:8, color:C.text,
             fontSize:13, outline:'none' }
  })

  const statusBadge = (t) => {
    const expired = new Date() > new Date(t.expiresAt)
    const s = expired ? 'expired' : t.status
    const map = { active:{ bg:C.green, text:'Faol' }, expired:{ bg:C.orange, text:'Tugagan' },
                  blocked:{ bg:C.red, text:'Bloklangan' } }
    const m = map[s] || map.active
    return <span style={{ background:m.bg, color:'#fff', fontSize:11,
                          padding:'3px 8px', borderRadius:4, fontWeight:600 }}>{m.text}</span>
  }

  return (
    <div style={{ minHeight:'100vh', background:C.bg, padding:24 }}>
      {/* Header */}
      <div style={{ display:'flex', justifyContent:'space-between', alignItems:'center', marginBottom:24 }}>
        <div>
          <h1 style={{ color:C.text, fontSize:22, fontWeight:800 }}>🔐 Super Admin Panel</h1>
          <p style={{ color:C.muted, fontSize:13, marginTop:4 }}>Barcha do'konlar boshqaruvi</p>
        </div>
        <div style={{ display:'flex', gap:8 }}>
          <button onClick={() => setShowForm(true)} style={{
            padding:'10px 20px', background:C.accent, border:'none',
            borderRadius:8, color:'#fff', fontWeight:700, cursor:'pointer'
          }}>➕ Yangi do'kon</button>
          <button onClick={logout} style={{
            padding:'10px 20px', background:'transparent', border:`1px solid ${C.border}`,
            borderRadius:8, color:C.muted, cursor:'pointer'
          }}>🚪 Chiqish</button>
        </div>
      </div>

      {/* Stats */}
      {stats && (
        <div style={{ display:'grid', gridTemplateColumns:'repeat(4,1fr)', gap:12, marginBottom:24 }}>
          {[
            { label:"Jami do'konlar", value:stats.totalTenants, color:C.text },
            { label:'Faol', value:stats.activeTenants, color:C.green },
            { label:'Tugagan', value:stats.expiredTenants, color:C.orange },
            { label:'Umumiy savdo', value:fmt(stats.totalRevenue)+' so\'m', color:C.accent },
          ].map(s => (
            <div key={s.label} style={{ background:C.card, borderRadius:12, padding:20,
                                        border:`1px solid ${C.border}` }}>
              <div style={{ color:C.muted, fontSize:11, fontWeight:600 }}>{s.label.toUpperCase()}</div>
              <div style={{ color:s.color, fontSize:26, fontWeight:800, marginTop:6 }}>{s.value}</div>
            </div>
          ))}
        </div>
      )}

      {/* Soon expiring warning */}
      {stats?.soonExpiring?.length > 0 && (
        <div style={{ background:'rgba(243,156,18,0.1)', border:`1px solid ${C.orange}`,
                      borderRadius:10, padding:14, marginBottom:16 }}>
          <span style={{ color:C.orange, fontWeight:700, fontSize:13 }}>
            ⚠️ {stats.soonExpiring.length} ta do'kon 7 kun ichida tugaydi:
          </span>
          <span style={{ color:C.muted, fontSize:13, marginLeft:8 }}>
            {stats.soonExpiring.map(t => `${t.name} (${new Date(t.expiresAt).toLocaleDateString()})`).join(' • ')}
          </span>
        </div>
      )}

      {/* Tenants table */}
      <div style={{ background:C.card, borderRadius:12, border:`1px solid ${C.border}`, overflow:'hidden' }}>
        <table style={{ width:'100%', borderCollapse:'collapse', fontSize:13 }}>
          <thead>
            <tr style={{ background:'#0f1117' }}>
              {["Do'kon","Slug","Tel","Muddat","Holat","Bugungi savdo","Amallar"].map(h => (
                <th key={h} style={{ padding:'12px 14px', textAlign:'left', color:C.muted,
                                     fontSize:11, fontWeight:600, letterSpacing:0.5 }}>{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {tenants.map(t => (
              <tr key={t.id} style={{ borderTop:`1px solid ${C.border}` }}>
                <td style={{ padding:'14px', color:C.text, fontWeight:600 }}>
                  {t.name}
                  {t.logoUrl && <img src={t.logoUrl} style={{ width:24, height:24, borderRadius:4,
                                                               marginLeft:8, verticalAlign:'middle' }} />}
                </td>
                <td style={{ padding:'14px', color:C.muted, fontFamily:'monospace' }}>{t.slug}</td>
                <td style={{ padding:'14px', color:C.muted }}>{t.phone || '—'}</td>
                <td style={{ padding:'14px' }}>
                  <div style={{ color:C.text, fontSize:12 }}>
                    {new Date(t.expiresAt).toLocaleDateString('uz-UZ')}
                  </div>
                  {(() => {
                    const days = Math.ceil((new Date(t.expiresAt) - new Date()) / 86400000)
                    return days > 0
                      ? <div style={{ color: days <= 7 ? C.orange : C.muted, fontSize:11 }}>{days} kun qoldi</div>
                      : <div style={{ color:C.red, fontSize:11 }}>Tugagan!</div>
                  })()}
                </td>
                <td style={{ padding:'14px' }}>{statusBadge(t)}</td>
                <td style={{ padding:'14px', color:C.accent, fontWeight:700 }}>
                  {fmt(t.todayRevenue)} so'm
                  <div style={{ color:C.muted, fontSize:11 }}>{t.todayTransactions} ta</div>
                </td>
                <td style={{ padding:'14px' }}>
                  <div style={{ display:'flex', gap:4, flexWrap:'wrap' }}>
                    {/* Extend */}
                    {extendId === t.id ? (
                      <div style={{ display:'flex', gap:4 }}>
                        <select value={extMonths} onChange={e => setExtMonths(e.target.value)}
                          style={{ padding:'4px 8px', background:'#0f1117', border:`1px solid ${C.border}`,
                                   borderRadius:6, color:C.text, fontSize:12 }}>
                          {[1,2,3,6,12].map(m => <option key={m} value={m}>{m} oy</option>)}
                        </select>
                        <button onClick={() => extend(t.id)} style={{
                          padding:'4px 10px', background:C.green, border:'none',
                          borderRadius:6, color:'#fff', fontSize:11, cursor:'pointer'
                        }}>✅</button>
                        <button onClick={() => setExtendId(null)} style={{
                          padding:'4px 10px', background:'transparent', border:`1px solid ${C.border}`,
                          borderRadius:6, color:C.muted, fontSize:11, cursor:'pointer'
                        }}>✕</button>
                      </div>
                    ) : (
                      <button onClick={() => setExtendId(t.id)} style={{
                        padding:'5px 10px', background:C.green, border:'none',
                        borderRadius:6, color:'#fff', fontSize:11, cursor:'pointer'
                      }}>📅 Uzaytirish</button>
                    )}

                    {t.status === 'blocked' ? (
                      <button onClick={() => setStatus(t.id, 'active')} style={{
                        padding:'5px 10px', background:C.accent, border:'none',
                        borderRadius:6, color:'#fff', fontSize:11, cursor:'pointer'
                      }}>✅ Aktiv</button>
                    ) : (
                      <button onClick={() => setStatus(t.id, 'blocked')} style={{
                        padding:'5px 10px', background:C.red, border:'none',
                        borderRadius:6, color:'#fff', fontSize:11, cursor:'pointer'
                      }}>🚫 Bloklash</button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Create tenant modal */}
      {showForm && (
        <div style={{ position:'fixed', inset:0, background:'rgba(0,0,0,0.8)',
                      display:'flex', alignItems:'center', justifyContent:'center', zIndex:100 }}>
          <div style={{ background:C.card, borderRadius:16, padding:32, width:480,
                        border:`1px solid ${C.border}`, maxHeight:'90vh', overflow:'auto' }}>
            <div style={{ display:'flex', justifyContent:'space-between', marginBottom:20 }}>
              <h2 style={{ color:C.text, fontSize:16, fontWeight:700 }}>➕ Yangi do'kon yaratish</h2>
              <button onClick={() => setShowForm(false)} style={{
                background:'transparent', border:'none', color:C.muted, fontSize:20, cursor:'pointer'
              }}>✕</button>
            </div>
            <form onSubmit={create} style={{ display:'flex', flexDirection:'column', gap:14 }}>
              {[
                { key:'name',  label:"DO'KON NOMI *" },
                { key:'slug',  label:'SLUG (URL uchun) *', placeholder:'bahor-market' },
                { key:'phone', label:'TELEFON' },
                { key:'address', label:'MANZIL' },
                { key:'adminUsername', label:'ADMIN USERNAME *' },
                { key:'adminPassword', label:'ADMIN PAROL *', type:'password' },
              ].map(f => (
                <div key={f.key}>
                  <label style={{ color:C.muted, fontSize:11, fontWeight:600,
                                  letterSpacing:1, display:'block', marginBottom:4 }}>{f.label}</label>
                  <input type={f.type||'text'} placeholder={f.placeholder}
                    required={f.label.includes('*')} {...inp(f.key)} />
                </div>
              ))}
              <div>
                <label style={{ color:C.muted, fontSize:11, fontWeight:600,
                                letterSpacing:1, display:'block', marginBottom:4 }}>OBUNA MUDDATI</label>
                <select {...inp('months')} style={{ ...inp('months').style }}>
                  {[1,2,3,6,12].map(m => <option key={m} value={m}>{m} oy</option>)}
                </select>
              </div>
              <button type="submit" disabled={loading} style={{
                padding:'12px', background:C.accent, border:'none', borderRadius:8,
                color:'#fff', fontWeight:700, cursor:'pointer', fontSize:14
              }}>{loading ? 'Yaratilmoqda...' : "✅ DO'KON YARATISH"}</button>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
