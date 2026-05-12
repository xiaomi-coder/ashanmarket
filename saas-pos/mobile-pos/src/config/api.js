import axios from 'axios';
import AsyncStorage from '@react-native-async-storage/async-storage';

const API_URL = 'http://sotuvpos.uz/api';

const api = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

api.interceptors.request.use(
  async (config) => {
    const token = await AsyncStorage.getItem('userToken');
    const slug = await AsyncStorage.getItem('tenantSlug');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    if (slug && !config.data?.slug) {
      // Slug is often passed in body for login, but we can also set it as a header or just ensure token is enough
      // For Supermarket POS, the token contains the tenant info usually.
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

export default api;
