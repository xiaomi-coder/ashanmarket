import React, { useState, useEffect } from 'react';
import { StyleSheet, Text, View, TouchableOpacity, SafeAreaView, FlatList, TextInput, KeyboardAvoidingView, Platform, ActivityIndicator, Alert } from 'react-native';
import { StatusBar } from 'expo-status-bar';
import api from '../config/api';

export default function ExpensesScreen({ navigation }) {
  const [expenses, setExpenses] = useState([]);
  const [loading, setLoading] = useState(true);
  
  const [newReason, setNewReason] = useState('');
  const [newCategory, setNewCategory] = useState('');
  const [newAmount, setNewAmount] = useState('');

  useEffect(() => {
    fetchExpenses();
  }, []);

  const fetchExpenses = async () => {
    try {
      const response = await api.get('/expenses');
      setExpenses(response.data);
    } catch (error) {
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  const addExpense = async () => {
    if (newReason.trim() && newAmount.trim() && newCategory.trim()) {
      try {
        const response = await api.post('/expenses', {
          reason: newReason,
          categoryName: newCategory,
          amount: newAmount
        });
        setExpenses([response.data, ...expenses]);
        setNewReason('');
        setNewCategory('');
        setNewAmount('');
        Alert.alert("Muvaffaqiyatli", "Xarajat saqlandi");
      } catch (error) {
        Alert.alert("Xatolik", "Xarajat qo'shishda xatolik yuz berdi");
      }
    } else {
      Alert.alert("Xatolik", "Barcha maydonlarni to'ldiring");
    }
  };

  const renderItem = ({ item }) => {
    const d = new Date(item.date);
    const dateStr = `${d.getDate().toString().padStart(2, '0')}.${(d.getMonth()+1).toString().padStart(2, '0')}.${d.getFullYear()} ${d.getHours().toString().padStart(2, '0')}:${d.getMinutes().toString().padStart(2, '0')}`;
    return (
      <View style={styles.card}>
        <View style={styles.cardInfo}>
          <Text style={styles.cardCategory}>{item.categoryName}</Text>
          <Text style={styles.cardTitle}>{item.reason}</Text>
          <Text style={styles.cardDate}>{dateStr} • {item.cashierName}</Text>
        </View>
        <Text style={styles.cardAmount}>- {parseInt(item.amount).toLocaleString('ru-RU')} so'm</Text>
      </View>
    );
  };

  return (
    <SafeAreaView style={styles.container}>
      <StatusBar style="light" />
      <View style={styles.header}>
        <TouchableOpacity style={styles.backButton} onPress={() => navigation.goBack()}>
          <Text style={styles.backText}>⬅ Orqaga</Text>
        </TouchableOpacity>
        <Text style={styles.title}>Xarajatlar</Text>
      </View>

      <KeyboardAvoidingView 
        behavior={Platform.OS === 'ios' ? 'padding' : 'height'} 
        style={styles.content}
        keyboardVerticalOffset={Platform.OS === 'ios' ? 20 : 20}
      >
        {loading ? (
          <ActivityIndicator size="large" color="#3498DB" style={{ marginTop: 50, flex: 1 }} />
        ) : (
          <FlatList
            data={expenses}
            keyExtractor={item => item.id.toString()}
            renderItem={renderItem}
            contentContainerStyle={styles.list}
            showsVerticalScrollIndicator={false}
            keyboardDismissMode="on-drag"
          />
        )}

        <View style={styles.addSection}>
          <Text style={styles.sectionTitle}>Yangi xarajat qo'shish</Text>
          
          <View style={styles.inputRow}>
            <TextInput
              style={[styles.input, { flex: 1, marginRight: 10 }]}
              placeholder="Toifa (Kategoriya)"
              placeholderTextColor="#7F8C8D"
              value={newCategory}
              onChangeText={setNewCategory}
            />
            <TextInput
              style={[styles.input, { flex: 1 }]}
              placeholder="Summasi"
              placeholderTextColor="#7F8C8D"
              keyboardType="numeric"
              value={newAmount}
              onChangeText={setNewAmount}
            />
          </View>
          
          <TextInput
            style={styles.input}
            placeholder="Nima uchun xarajat? (Izoh)"
            placeholderTextColor="#7F8C8D"
            value={newReason}
            onChangeText={setNewReason}
          />
          
          <TouchableOpacity style={styles.addButton} onPress={addExpense}>
            <Text style={styles.addButtonText}>Qo'shish</Text>
          </TouchableOpacity>
        </View>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#1E232E' },
  header: { 
    flexDirection: 'row', 
    alignItems: 'center', 
    padding: 20, 
    paddingTop: 50, 
    backgroundColor: '#2C3240',
    elevation: 4,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.2,
    shadowRadius: 4,
  },
  backButton: { marginRight: 20 },
  backText: { color: '#3498DB', fontSize: 16, fontWeight: 'bold' },
  title: { color: 'white', fontSize: 20, fontWeight: 'bold' },
  content: { flex: 1 },
  list: { padding: 20 },
  card: {
    backgroundColor: '#2C3240',
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  cardInfo: { flex: 1, paddingRight: 10 },
  cardCategory: { color: '#3498DB', fontSize: 12, fontWeight: 'bold', marginBottom: 2 },
  cardTitle: { color: '#ECF0F1', fontSize: 16, fontWeight: 'bold', marginBottom: 4 },
  cardDate: { color: '#7F8C8D', fontSize: 12 },
  cardAmount: { color: '#E74C3C', fontSize: 16, fontWeight: 'bold' },
  addSection: {
    backgroundColor: '#2C3240',
    padding: 20,
    borderTopLeftRadius: 24,
    borderTopRightRadius: 24,
    elevation: 10,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: -4 },
    shadowOpacity: 0.2,
    shadowRadius: 8,
  },
  sectionTitle: { color: '#ECF0F1', fontSize: 16, fontWeight: 'bold', marginBottom: 15 },
  inputRow: { flexDirection: 'row', justifyContent: 'space-between' },
  input: {
    backgroundColor: '#1E232E',
    borderRadius: 8,
    color: '#ECF0F1',
    padding: 12,
    marginBottom: 10,
    fontSize: 14,
  },
  addButton: {
    backgroundColor: '#3498DB',
    borderRadius: 8,
    padding: 15,
    alignItems: 'center',
    marginTop: 5,
  },
  addButtonText: { color: 'white', fontWeight: 'bold', fontSize: 16 }
});
