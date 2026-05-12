import React, { useState, useEffect } from 'react';
import { StyleSheet, Text, View, TouchableOpacity, SafeAreaView, FlatList, TextInput, KeyboardAvoidingView, Platform, Modal, Alert, ActivityIndicator } from 'react-native';
import { StatusBar } from 'expo-status-bar';
import api from '../config/api';

export default function DebtsScreen({ navigation }) {
  const [debts, setDebts] = useState([]);
  const [loading, setLoading] = useState(true);
  
  const [newName, setNewName] = useState('');
  const [newPhone, setNewPhone] = useState('');
  const [newAmount, setNewAmount] = useState('');
  const [newNotes, setNewNotes] = useState('');

  // Payment Modal State
  const [paymentModalVisible, setPaymentModalVisible] = useState(false);
  const [selectedDebt, setSelectedDebt] = useState(null);
  const [paymentAmount, setPaymentAmount] = useState('');

  useEffect(() => {
    fetchDebts();
  }, []);

  const fetchDebts = async () => {
    try {
      const response = await api.get('/debts');
      setDebts(response.data);
    } catch (error) {
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  const addDebt = async () => {
    if (newName.trim() && newAmount.trim()) {
      try {
        await api.post('/debts', {
          name: newName,
          phone: newPhone,
          amount: newAmount,
          type: 'borrow'
        });
        fetchDebts();
        setNewName('');
        setNewPhone('');
        setNewAmount('');
        setNewNotes('');
        Alert.alert("Muvaffaqiyatli", "Qarz yozildi");
      } catch (error) {
        Alert.alert("Xatolik", "Qarz qo'shishda xatolik yuz berdi");
      }
    } else {
      Alert.alert("Xatolik", "Ism va summani kiriting");
    }
  };

  const openPaymentModal = (debt) => {
    setSelectedDebt(debt);
    setPaymentAmount(debt.totalDebt.toString());
    setPaymentModalVisible(true);
  };

  const processPayment = async () => {
    const payAmt = parseInt(paymentAmount);
    if (!payAmt || payAmt <= 0) {
      Alert.alert('Xato', 'To\'g\'ri summa kiriting');
      return;
    }

    if (payAmt > selectedDebt.totalDebt) {
      Alert.alert('Xato', 'Kiritilgan summa qarzdan ko\'p!');
      return;
    }

    try {
      await api.post('/debts', {
        name: selectedDebt.name,
        amount: payAmt,
        type: 'repay'
      });
      fetchDebts();
      setPaymentModalVisible(false);
      setSelectedDebt(null);
      setPaymentAmount('');
      Alert.alert("Muvaffaqiyatli", "Qarz to'landi");
    } catch (error) {
      Alert.alert("Xatolik", "To'lovni saqlashda xatolik");
    }
  };

  const renderItem = ({ item }) => {
    const isOverdue = false; // Add logic if needed based on dates
    const d = new Date(item.createdAt);
    const dateStr = `${d.getDate()}.${d.getMonth()+1}.${d.getFullYear()}`;
    return (
      <View style={styles.card}>
        <View style={styles.cardInfo}>
          <View style={styles.nameRow}>
            <Text style={styles.cardName}>{item.name}</Text>
            {isOverdue && <View style={styles.overdueBadge}><Text style={styles.overdueText}>Kechikkan</Text></View>}
          </View>
          <Text style={styles.cardPhone}>{item.phone || "Raqam yo'q"}</Text>
          <Text style={styles.cardDate}>{dateStr}</Text>
          <Text style={styles.cardAmount}>{item.totalDebt.toLocaleString('ru-RU')} so'm</Text>
        </View>
        <TouchableOpacity style={styles.payBtn} onPress={() => openPaymentModal(item)}>
          <Text style={styles.payBtnText}>To'lash</Text>
        </TouchableOpacity>
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
        <Text style={styles.title}>Qarzlar Daftari</Text>
      </View>

      <KeyboardAvoidingView 
        behavior={Platform.OS === 'ios' ? 'padding' : undefined} 
        style={styles.content}
        keyboardVerticalOffset={Platform.OS === 'ios' ? 20 : 0}
      >
        <View style={styles.statsCard}>
          <Text style={styles.statsLabel}>Jami berilgan qarzlar qoldig'i</Text>
          <Text style={styles.statsValue}>
            {debts.reduce((sum, item) => sum + item.totalDebt, 0).toLocaleString('ru-RU')} so'm
          </Text>
        </View>

        {loading ? (
          <ActivityIndicator size="large" color="#3498DB" style={{ marginTop: 50, flex: 1 }} />
        ) : (
          <FlatList
            data={debts}
            keyExtractor={item => item.id.toString()}
            renderItem={renderItem}
            contentContainerStyle={styles.list}
            showsVerticalScrollIndicator={false}
            keyboardDismissMode="on-drag"
          />
        )}

        <View style={styles.addSection}>
          <Text style={styles.sectionTitle}>Yangi qarz berish</Text>
          <View style={styles.inputRow}>
            <TextInput
              style={[styles.input, { flex: 1.5, marginRight: 10 }]}
              placeholder="Mijoz ismi"
              placeholderTextColor="#7F8C8D"
              value={newName}
              onChangeText={setNewName}
            />
            <TextInput
              style={[styles.input, { flex: 1 }]}
              placeholder="Telefon"
              placeholderTextColor="#7F8C8D"
              keyboardType="phone-pad"
              value={newPhone}
              onChangeText={setNewPhone}
            />
          </View>
          <View style={styles.inputRow}>
            <TextInput
              style={[styles.input, { flex: 1, marginRight: 10 }]}
              placeholder="Qarz summasi"
              placeholderTextColor="#7F8C8D"
              keyboardType="numeric"
              value={newAmount}
              onChangeText={setNewAmount}
            />
            <TextInput
              style={[styles.input, { flex: 1.5 }]}
              placeholder="Izoh (Notes)"
              placeholderTextColor="#7F8C8D"
              value={newNotes}
              onChangeText={setNewNotes}
            />
          </View>
          <TouchableOpacity style={styles.addButton} onPress={addDebt}>
            <Text style={styles.addButtonText}>Qarz yozish</Text>
          </TouchableOpacity>
        </View>
      </KeyboardAvoidingView>

      {/* Partial Payment Modal */}
      <Modal
        visible={paymentModalVisible}
        transparent={true}
        animationType="slide"
        onRequestClose={() => setPaymentModalVisible(false)}
      >
        <View style={styles.modalOverlay}>
          <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : undefined} style={styles.modalContent}>
            <Text style={styles.modalTitle}>Qarzni to'lash</Text>
            {selectedDebt && (
              <Text style={styles.modalSubtitle}>{selectedDebt.name} - Qoldiq: {selectedDebt.amount.toLocaleString('ru-RU')} so'm</Text>
            )}
            <Text style={styles.label}>Qancha summa to'lanyapti?</Text>
            <TextInput
              style={styles.modalInput}
              keyboardType="numeric"
              value={paymentAmount}
              onChangeText={setPaymentAmount}
              autoFocus
            />
            <View style={styles.modalActions}>
              <TouchableOpacity style={[styles.modalBtn, styles.modalCancelBtn]} onPress={() => setPaymentModalVisible(false)}>
                <Text style={styles.modalCancelText}>Bekor qilish</Text>
              </TouchableOpacity>
              <TouchableOpacity style={[styles.modalBtn, styles.modalSubmitBtn]} onPress={processPayment}>
                <Text style={styles.modalSubmitText}>Qabul qilish</Text>
              </TouchableOpacity>
            </View>
          </KeyboardAvoidingView>
        </View>
      </Modal>

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
  statsCard: {
    backgroundColor: '#3498DB',
    margin: 20,
    marginBottom: 0,
    padding: 20,
    borderRadius: 16,
    alignItems: 'center',
    elevation: 6,
    shadowColor: '#3498DB',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.3,
    shadowRadius: 8,
  },
  statsLabel: { color: 'rgba(255,255,255,0.8)', fontSize: 14, fontWeight: 'bold' },
  statsValue: { color: '#FFF', fontSize: 28, fontWeight: 'bold', marginTop: 5 },
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
  cardInfo: { flex: 1 },
  nameRow: { flexDirection: 'row', alignItems: 'center', marginBottom: 2 },
  cardName: { color: '#ECF0F1', fontSize: 16, fontWeight: 'bold', marginRight: 8 },
  overdueBadge: { backgroundColor: '#E74C3C', paddingHorizontal: 6, paddingVertical: 2, borderRadius: 4 },
  overdueText: { color: 'white', fontSize: 10, fontWeight: 'bold' },
  cardPhone: { color: '#7F8C8D', fontSize: 12, marginBottom: 2 },
  cardDate: { color: '#7F8C8D', fontSize: 12, marginBottom: 8 },
  cardAmount: { color: '#F1C40F', fontSize: 18, fontWeight: 'bold' },
  payBtn: {
    backgroundColor: 'rgba(46, 204, 113, 0.2)',
    paddingVertical: 10,
    paddingHorizontal: 16,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#2ECC71',
    marginLeft: 10,
  },
  payBtnText: { color: '#2ECC71', fontWeight: 'bold' },
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
    backgroundColor: '#F1C40F',
    borderRadius: 8,
    padding: 15,
    alignItems: 'center',
    marginTop: 5,
  },
  addButtonText: { color: '#1E232E', fontWeight: 'bold', fontSize: 16 },
  
  // Modal styles
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.6)',
    justifyContent: 'flex-end',
  },
  modalContent: {
    backgroundColor: '#2C3240',
    borderTopLeftRadius: 24,
    borderTopRightRadius: 24,
    padding: 24,
    paddingBottom: Platform.OS === 'ios' ? 40 : 24,
  },
  modalTitle: { color: 'white', fontSize: 20, fontWeight: 'bold', marginBottom: 5 },
  modalSubtitle: { color: '#7F8C8D', fontSize: 14, marginBottom: 20 },
  label: { color: '#BDC3C7', fontSize: 14, marginBottom: 8, fontWeight: 'bold' },
  modalInput: {
    backgroundColor: '#1E232E',
    borderRadius: 8,
    color: '#F1C40F',
    padding: 16,
    fontSize: 24,
    fontWeight: 'bold',
    marginBottom: 24,
    textAlign: 'center',
  },
  modalActions: { flexDirection: 'row', justifyContent: 'space-between' },
  modalBtn: { flex: 1, padding: 15, borderRadius: 8, alignItems: 'center' },
  modalCancelBtn: { backgroundColor: '#1E232E', marginRight: 10 },
  modalSubmitBtn: { backgroundColor: '#2ECC71', marginLeft: 10 },
  modalCancelText: { color: '#ECF0F1', fontWeight: 'bold', fontSize: 16 },
  modalSubmitText: { color: 'white', fontWeight: 'bold', fontSize: 16 },
});
