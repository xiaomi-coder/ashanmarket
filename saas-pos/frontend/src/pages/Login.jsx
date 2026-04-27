import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../lib/api.js'
import toast from 'react-hot-toast'

export default function Login() {
  const navigate = useNavigate()
  const [form, setForm] = useState({ slug: '', username: '', password: '' })
  const [loading, setLoading] = useState(false)

  const login = async (e) => {
    e.preventDefault()
    setLoading(true)
    try {
      const { data } = await api.post('/api/auth/login', form)
      localStorage.setItem('token', data.token)
      localStorage.setItem('user', JSON.stringify(data.user))
      localStorage.setItem('tenant', JSON.stringify(data.tenant))
      toast.success(`Xush kelibsiz, ${data.user.fullName}!`)
      if (data.user.role === 'admin') {
        navigate('/dashboard')
      } else {
        navigate('/cashier')
      }
    } catch (err) {
      toast.error(err.response?.data?.error || 'Xatolik yuz berdi')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div style={{ minHeight:'100vh', display:'flex', alignItems:'center',
                  justifyContent:'center', background:'#0f1117' }}>
      <div style={{ width:400, background:'#1a1d27', borderRadius:16,
                    border:'1px solid #2e3460', padding:40 }}>
        <div style={{ textAlign:'center', marginBottom:32 }}>
          <div style={{ fontSize:52 }}>🛒</div>
          <h1 style={{ fontSize:22, fontWeight:800, color:'#f0f2ff', marginTop:8 }}>
            SuperMarket POS
          </h1>
          <p style={{ color:'#8892b0', fontSize:13, marginTop:4 }}>Tizimga kiring</p>
        </div>

        <form onSubmit={login}>
          {[
            { key:'slug',     label:"DO'KON KODI",  placeholder:"masalan: bahor-market" },
            { key:'username', label:"FOYDALANUVCHI", placeholder:"username" },
            { key:'password', label:"PAROL",         placeholder:"••••••••", type:'password' }
          ].map(f => (
            <div key={f.key} style={{ marginBottom:16 }}>
              <label style={{ display:'block', color:'#8892b0', fontSize:11,
                              fontWeight:600, marginBottom:6, letterSpacing:1 }}>
                {f.label}
              </label>
              <input
                type={f.type || 'text'}
                placeholder={f.placeholder}
                value={form[f.key]}
                onChange={e => setForm(p => ({ ...p, [f.key]: e.target.value }))}
                required
                style={{ width:'100%', padding:'11px 14px', background:'#0f1117',
                         border:'1px solid #2e3460', borderRadius:8, color:'#f0f2ff',
                         fontSize:14, outline:'none' }}
              />
            </div>
          ))}

          <button type="submit" disabled={loading} style={{
            width:'100%', padding:'13px', background:'#3d7fff',
            border:'none', borderRadius:8, color:'#fff', fontSize:14,
            fontWeight:700, cursor:'pointer', marginTop:8,
            opacity: loading ? 0.7 : 1
          }}>
            {loading ? 'Kirish...' : 'KIRISH'}
          </button>
        </form>

        <p style={{ textAlign:'center', color:'#8892b0', fontSize:11, marginTop:20 }}>
          Do'kon kodini administrator beradi
        </p>
      </div>
    </div>
  )
}
