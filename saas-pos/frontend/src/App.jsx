import { Routes, Route, Navigate } from 'react-router-dom'
import Login from './pages/Login.jsx'
import Dashboard from './pages/Dashboard.jsx'
import Cashier from './pages/Cashier.jsx'
import Products from './pages/Products.jsx'
import Reports from './pages/Reports.jsx'
import SuperAdmin from './pages/SuperAdmin.jsx'
import SuperLogin from './pages/SuperLogin.jsx'
import Layout from './components/Layout.jsx'

const PrivateRoute = ({ children }) => {
  const token = localStorage.getItem('token')
  return token ? children : <Navigate to="/login" />
}

const SuperRoute = ({ children }) => {
  const token = localStorage.getItem('superToken')
  return token ? children : <Navigate to="/super-login" />
}

export default function App() {
  return (
    <Routes>
      {/* Public */}
      <Route path="/login" element={<Login />} />
      <Route path="/super-login" element={<SuperLogin />} />

      {/* Tenant routes */}
      <Route path="/" element={<PrivateRoute><Layout /></PrivateRoute>}>
        <Route index element={<Navigate to="/cashier" />} />
        <Route path="cashier"  element={<Cashier />} />
        <Route path="dashboard" element={<Dashboard />} />
        <Route path="products" element={<Products />} />
        <Route path="reports"  element={<Reports />} />
      </Route>

      {/* Super Admin */}
      <Route path="/super" element={<SuperRoute><SuperAdmin /></SuperRoute>} />

      <Route path="*" element={<Navigate to="/" />} />
    </Routes>
  )
}
