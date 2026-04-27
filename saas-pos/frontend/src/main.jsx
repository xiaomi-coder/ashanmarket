import React from 'react'
import ReactDOM from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { Toaster } from 'react-hot-toast'
import App from './App.jsx'

ReactDOM.createRoot(document.getElementById('root')).render(
  <BrowserRouter>
    <App />
    <Toaster
      position="top-right"
      toastOptions={{
        style: { background: '#22263a', color: '#f0f2ff', border: '1px solid #2e3460' },
        success: { iconTheme: { primary: '#27ae60', secondary: '#fff' } },
        error: { iconTheme: { primary: '#e74c3c', secondary: '#fff' } }
      }}
    />
  </BrowserRouter>
)
