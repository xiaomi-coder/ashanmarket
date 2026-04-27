import { Outlet, NavLink, useNavigate } from 'react-router-dom'
import { useState, useEffect } from 'react'

const s = {
  shell:    { display:'flex', height:'100vh', background:'#0f1117' },
  sidebar:  { width:220, background:'#1a1d27', borderRight:'1px solid #2e3460', display:'flex', flexDirection:'column' },
  logo:     { padding:'24px 20px 20px', borderBottom:'1px solid #2e3460' },
  logoText: { fontSize:18, fontWeight:800, color:'#f0f2ff', letterSpacing:-0.5 },
  logoSub:  { fontSize:12, color:'#8892b0', marginTop:4 },
  nav:      { padding:'16px 8px', flex:1 },
  navLink:  (active) => ({
    display:'flex', alignItems:'center', gap:10, padding:'11px 14px',
    borderRadius:8, marginBottom:4, textDecoration:'none', fontSize:13, fontWeight:500,
    color: active ? '#f0f2ff' : '#8892b0',
    background: active ? '#22263a' : 'transparent',
    transition:'all 0.15s'
  }),
  bottom:   { padding:'16px 8px', borderTop:'1px solid #2e3460' },
  content:  { flex:1, overflow:'auto' },
  badge:    { background:'#f39c12', color:'#fff', fontSize:10, padding:'2px 7px', borderRadius:10, marginLeft:'auto' }
}

export default function Layout() {
  const navigate = useNavigate()
  const [user, setUser] = useState(null)
  const [tenant, setTenant] = useState(null)
  const [daysLeft, setDaysLeft] = useState(null)

  useEffect(() => {
    try {
      setUser(JSON.parse(localStorage.getItem('user') || 'null'))
      const t = JSON.parse(localStorage.getItem('tenant') || 'null')
      setTenant(t)
      if (t?.expiresAt) {
        const days = Math.ceil((new Date(t.expiresAt) - new Date()) / 86400000)
        setDaysLeft(days)
      }
    } catch {}
  }, [])

  const logout = () => {
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    localStorage.removeItem('tenant')
    navigate('/login')
  }

  const isAdmin = user?.role === 'admin'

  const links = [
    { to:'/cashier',   icon:'🏪', label:'Kassir', cashierOnly: true },
    { to:'/dashboard', icon:'📊', label:'Dashboard', admin: true },
    { to:'/products',  icon:'📦', label:'Mahsulotlar', admin: true },
    { to:'/reports',   icon:'📈', label:'Hisobotlar', admin: true },
  ].filter(l => (isAdmin && l.admin) || (!isAdmin && l.cashierOnly))

  return (
    <div style={s.shell}>
      <aside style={s.sidebar}>
        <div style={s.logo}>
          {tenant?.logoUrl && (
            <img src={tenant.logoUrl} alt="logo"
                 style={{ width:40, height:40, borderRadius:8, marginBottom:8, objectFit:'cover' }} />
          )}
          <div style={s.logoText}>🛒 {tenant?.name || 'POS'}</div>
          <div style={s.logoSub}>👤 {user?.fullName}</div>
          {daysLeft !== null && daysLeft <= 7 && (
            <div style={{ marginTop:8, background:'#f39c12', color:'#fff', fontSize:11,
                          padding:'4px 8px', borderRadius:6, fontWeight:600 }}>
              ⚠️ {daysLeft} kun qoldi
            </div>
          )}
        </div>

        <nav style={s.nav}>
          {links.map(l => (
            <NavLink key={l.to} to={l.to} style={({ isActive }) => s.navLink(isActive)}>
              <span style={{ fontSize:16 }}>{l.icon}</span>
              {l.label}
            </NavLink>
          ))}
        </nav>

        <div style={s.bottom}>
          <button onClick={logout} style={{
            width:'100%', padding:'10px 14px', background:'transparent',
            border:'1px solid #2e3460', borderRadius:8, color:'#8892b0',
            fontSize:13, cursor:'pointer', textAlign:'left'
          }}>
            🚪 Chiqish
          </button>
        </div>
      </aside>

      <main style={s.content}>
        <Outlet />
      </main>
    </div>
  )
}
