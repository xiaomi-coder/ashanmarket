import React, { useState, useContext } from 'react';
import { StyleSheet, Text, View, TextInput, TouchableOpacity, SafeAreaView, KeyboardAvoidingView, Platform, ActivityIndicator, Alert } from 'react-native';
import { StatusBar } from 'expo-status-bar';
import { AuthContext } from '../context/AuthContext';

export default function LoginScreen() {
  const [slug, setSlug] = useState('');
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const { login } = useContext(AuthContext);
  const [localLoading, setLocalLoading] = useState(false);

  const handleLogin = async () => {
    if (!slug || !username || !password) {
      Alert.alert("Xato", "Barcha maydonlarni to'ldiring!");
      return;
    }
    setLocalLoading(true);
    const res = await login(slug, username, password);
    setLocalLoading(false);
    
    if (!res.success) {
      Alert.alert("Xatolik", res.message);
    }
  };

  return (
    <SafeAreaView style={styles.container}>
      <StatusBar style="light" />
      <KeyboardAvoidingView 
        behavior={Platform.OS === 'ios' ? 'padding' : undefined} 
        style={styles.content}
      >
        
        <View style={styles.header}>
          <Text style={styles.title}>🛒 ASHAN MARKET</Text>
          <Text style={styles.subtitle}>Mobile Boshqaruv</Text>
        </View>

        <View style={styles.form}>
          <Text style={styles.label}>Do'kon Kodi (Slug)</Text>
          <TextInput 
            style={styles.input}
            placeholder="ashanmarket"
            placeholderTextColor="#7F8C8D"
            value={slug}
            onChangeText={setSlug}
            autoCapitalize="none"
          />

          <Text style={styles.label}>Loginingiz</Text>
          <TextInput 
            style={styles.input}
            placeholder="admin"
            placeholderTextColor="#7F8C8D"
            value={username}
            onChangeText={setUsername}
            autoCapitalize="none"
          />

          <Text style={styles.label}>Parolingiz</Text>
          <TextInput 
            style={styles.input}
            placeholder="••••••"
            placeholderTextColor="#7F8C8D"
            secureTextEntry
            value={password}
            onChangeText={setPassword}
          />

          <TouchableOpacity style={styles.button} onPress={handleLogin} disabled={localLoading}>
            {localLoading ? (
              <ActivityIndicator color="white" />
            ) : (
              <Text style={styles.buttonText}>Tizimga kirish</Text>
            )}
          </TouchableOpacity>
        </View>
        
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#1E232E',
  },
  content: {
    flex: 1,
    justifyContent: 'center',
    padding: 24,
  },
  header: {
    alignItems: 'center',
    marginBottom: 48,
  },
  title: {
    fontSize: 28,
    fontWeight: 'bold',
    color: '#ECF0F1',
    marginBottom: 8,
  },
  subtitle: {
    fontSize: 16,
    color: '#3498DB',
    fontWeight: 'bold',
  },
  form: {
    backgroundColor: '#2C3240',
    padding: 24,
    borderRadius: 16,
    elevation: 4,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.2,
    shadowRadius: 8,
  },
  label: {
    color: '#BDC3C7',
    fontSize: 14,
    marginBottom: 8,
    fontWeight: 'bold',
  },
  input: {
    backgroundColor: '#1E232E',
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#34495E',
    color: '#ECF0F1',
    paddingHorizontal: 16,
    paddingVertical: 12,
    fontSize: 16,
    marginBottom: 20,
  },
  button: {
    backgroundColor: '#3498DB',
    borderRadius: 8,
    paddingVertical: 14,
    alignItems: 'center',
    marginTop: 8,
  },
  buttonText: {
    color: '#FFFFFF',
    fontSize: 16,
    fontWeight: 'bold',
  },
});
