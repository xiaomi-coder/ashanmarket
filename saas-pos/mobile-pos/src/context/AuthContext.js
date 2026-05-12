import React, { createContext, useState, useEffect } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import api from '../config/api';

export const AuthContext = createContext();

export const AuthProvider = ({ children }) => {
  const [isLoading, setIsLoading] = useState(false);
  const [userToken, setUserToken] = useState(null);
  const [userInfo, setUserInfo] = useState(null);

  const login = async (slug, username, password) => {
    setIsLoading(true);
    try {
      const response = await api.post('/auth/login', {
        slug,
        username,
        password,
      });

      if (response.data && response.data.token) {
        let token = response.data.token;
        let info = response.data.user;

        setUserInfo(info);
        setUserToken(token);

        await AsyncStorage.setItem('userToken', token);
        await AsyncStorage.setItem('userInfo', JSON.stringify(info));
        await AsyncStorage.setItem('tenantSlug', slug);
        return { success: true };
      } else {
        return { success: false, message: 'Xato login yoki parol' };
      }
    } catch (e) {
      console.log(`Login error: ${e}`);
      return { success: false, message: e.response?.data?.error || 'Tarmoq xatosi' };
    } finally {
      setIsLoading(false);
    }
  };

  const logout = async () => {
    setIsLoading(true);
    setUserToken(null);
    setUserInfo(null);
    await AsyncStorage.removeItem('userToken');
    await AsyncStorage.removeItem('userInfo');
    setIsLoading(false);
  };

  const isLoggedIn = async () => {
    try {
      setIsLoading(true);
      let token = await AsyncStorage.getItem('userToken');
      let info = await AsyncStorage.getItem('userInfo');
      if (token) {
        setUserToken(token);
        if (info) setUserInfo(JSON.parse(info));
      }
    } catch (e) {
      console.log(`isLogged in error ${e}`);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    isLoggedIn();
  }, []);

  return (
    <AuthContext.Provider value={{ login, logout, isLoading, userToken, userInfo }}>
      {children}
    </AuthContext.Provider>
  );
};
