import axios from 'axios'

const API_URL = import.meta.env.VITE_API_URL || ''

export const api = axios.create({ baseURL: API_URL })

api.interceptors.request.use(config => {
  const token = localStorage.getItem('token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

api.interceptors.response.use(
  res => res,
  err => {
    if (err.response?.status === 401) {
      localStorage.clear()
      window.location.href = '/login'
    }
    return Promise.reject(err)
  }
)

export const superApi = axios.create({ baseURL: API_URL })
superApi.interceptors.request.use(config => {
  const token = localStorage.getItem('superToken')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})
