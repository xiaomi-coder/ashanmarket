import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { superApi } from '../lib/api.js'
import toast from 'react-hot-toast'

export default function SuperLogin() {
  const navigate = useNavigate()
  const [form, setForm] = useState({ username:'', password:'' })
  const [loading, setLoading] = useState(false)

  const login = async (e) => {
    e.preventDefault(); setLoading(true)
    try {
      const { data } = await superApi.post('/api/auth/super-login', form)
      localStorage.setItem('superToken', data.token)
      toast.success('Super Admin paneliga xush kelibsiz!')
      navigate('/super')
    } catch (err) {
      toast.error(err.response?.data?.error || "Noto'g'ri ma'lumotlar")
    } finally { setLoading(false) }
  }

  return (
    <div style={{ minHeight:'100vh', display:'flex', alignItems:'center',
                  justifyContent:'center', background:'#0f1117' }}>
      <div style={{ width:380, background:'#1a1d27', borderRadius:16,
                    border:'1px solid #2e3460', padding:40 }}>
        <div style={{ textAlign:'center', marginBottom:32 }}>
          <div style={{ fontSize:48 }}>🔐</div>
          <h1 style={{ fontSize:20, fontWeight:800, color:'#f0f2ff', marginTop:8 }}>Super Admin</h1>
          <p style={{ color:'#8892b0', fontSize:12, marginTop:4 }}>Faqat tizim administratori</p>
        </div>
        <form onSubmit={login} style={{ display:'flex', flexDirection:'column', gap:14 }}>
          {[
            { key:'username', label:'USERNAME', type:'text' },
            { key:'password', label:'PAROL', type:'password' }
          ].map(f => (
            <div key={f.key}>
              <label style={{ color:'#8892b0', fontSize:11, fontWeight:600,
                              letterSpacing:1, display:'block', marginBottom:6 }}>{f.label}</label>
              <input type={f.type} value={form[f.key]} required
                onChange={e => setForm(p => ({ ...p, [f.key]: e.target.value }))}
                style={{ width:'100%', padding:'11px 14px', background:'#0f1117',
                         border:'1px solid #2e3460', borderRadius:8, color:'#f0f2ff',
                         fontSize:14, outline:'none' }}
              />
            </div>
          ))}
          <button type="submit" disabled={loading} style={{
            padding:'13px', background:'#3d7fff', border:'none', borderRadius:8,
            color:'#fff', fontSize:14, fontWeight:700, cursor:'pointer',
            marginTop:4, opacity: loading ? 0.7 : 1
          }}>{loading ? 'Kirish...' : 'KIRISH'}</button>
        </form>
        <p style={{ textAlign:'center', marginTop:16, fontSize:11, color:'#8892b0' }}>
          <a href="/login" style={{ color:'#3d7fff', textDecoration:'none' }}>
            ← Kassir kirishiga qaytish
          </a>
        </p>
      </div>
    </div>
  )
}
