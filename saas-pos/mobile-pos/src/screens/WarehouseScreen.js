import React, { useState } from 'react';
import { StyleSheet, Text, View, TouchableOpacity, SafeAreaView, FlatList, TextInput, KeyboardAvoidingView, Platform } from 'react-native';
import { StatusBar } from 'expo-status-bar';

export default function WarehouseScreen({ navigation }) {
  const [products, setProducts] = useState([
    { id: '1', name: 'Coca-Cola 1.5L', barcode: '4321234567890', quantity: 45, unit: 'dona', price: '12000', costPrice: '9000', category: 'Ichimliklar' },
    { id: '2', name: 'Snickers', barcode: '5432109876543', quantity: 8, unit: 'dona', price: '8000', costPrice: '6500', category: 'Shirinliklar' },
    { id: '3', name: 'Kartoshka', barcode: '200000000001', quantity: 15.5, unit: 'kg', price: '4000', costPrice: '2500', category: 'Sabzavotlar' },
  ]);
  
  const [searchQuery, setSearchQuery] = useState('');

  const renderItem = ({ item }) => {
    const isLowStock = item.quantity <= 10;
    
    return (
      <View style={styles.card}>
        <View style={styles.cardInfo}>
          <Text style={styles.cardCategory}>{item.category}</Text>
          <Text style={styles.cardName}>{item.name}</Text>
          <Text style={styles.cardBarcode}>Shtrix kod: {item.barcode}</Text>
          <View style={styles.priceRow}>
            <Text style={styles.cardPrice}>{parseInt(item.price).toLocaleString('ru-RU')} so'm</Text>
            <Text style={styles.cardCostPrice}>Kelish: {parseInt(item.costPrice).toLocaleString('ru-RU')} so'm</Text>
          </View>
        </View>
        <View style={[styles.quantityBadge, isLowStock && styles.quantityBadgeLow]}>
          <Text style={styles.quantityText}>{item.quantity} {item.unit}</Text>
        </View>
      </View>
    );
  };

  const filteredProducts = products.filter(p => 
    p.name.toLowerCase().includes(searchQuery.toLowerCase()) || 
    p.barcode.includes(searchQuery)
  );

  return (
    <SafeAreaView style={styles.container}>
      <StatusBar style="light" />
      <View style={styles.header}>
        <TouchableOpacity style={styles.backButton} onPress={() => navigation.goBack()}>
          <Text style={styles.backText}>⬅ Orqaga</Text>
        </TouchableOpacity>
        <Text style={styles.title}>Ombor qoldig'i</Text>
      </View>

      <KeyboardAvoidingView 
        behavior={Platform.OS === 'ios' ? 'padding' : undefined} 
        style={styles.content}
      >
        <View style={styles.searchSection}>
          <TextInput
            style={styles.searchInput}
            placeholder="Nomi yoki shtrix kodini qidiring..."
            placeholderTextColor="#7F8C8D"
            value={searchQuery}
            onChangeText={setSearchQuery}
          />
          <TouchableOpacity style={styles.scanBtn} onPress={() => navigation.navigate('Scanner')}>
            <Text style={styles.scanBtnText}>📷</Text>
          </TouchableOpacity>
        </View>

        <View style={styles.statsRow}>
          <Text style={styles.statsText}>Jami turlar: {products.length}</Text>
          <Text style={styles.statsText}>Jami miqdor: {products.reduce((s, p) => s + p.quantity, 0)}</Text>
        </View>

        <FlatList
          data={filteredProducts}
          keyExtractor={item => item.id}
          renderItem={renderItem}
          contentContainerStyle={styles.list}
          showsVerticalScrollIndicator={false}
          keyboardDismissMode="on-drag"
          ListEmptyComponent={
            <Text style={styles.emptyText}>Mahsulot topilmadi</Text>
          }
        />
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
  searchSection: {
    flexDirection: 'row',
    padding: 20,
    paddingBottom: 10,
    backgroundColor: '#1E232E',
  },
  searchInput: {
    flex: 1,
    backgroundColor: '#2C3240',
    borderRadius: 8,
    color: '#ECF0F1',
    padding: 12,
    fontSize: 14,
    marginRight: 10,
  },
  scanBtn: {
    backgroundColor: '#3498DB',
    borderRadius: 8,
    padding: 12,
    justifyContent: 'center',
    alignItems: 'center',
    width: 50,
  },
  scanBtnText: { fontSize: 20 },
  statsRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingHorizontal: 20,
    paddingBottom: 10,
  },
  statsText: { color: '#BDC3C7', fontSize: 14, fontWeight: 'bold' },
  list: { padding: 20, paddingTop: 10 },
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
  cardCategory: { color: '#3498DB', fontSize: 12, fontWeight: 'bold', marginBottom: 2 },
  cardName: { color: '#ECF0F1', fontSize: 16, fontWeight: 'bold', marginBottom: 2 },
  cardBarcode: { color: '#7F8C8D', fontSize: 12, marginBottom: 8 },
  priceRow: { flexDirection: 'row', alignItems: 'center' },
  cardPrice: { color: '#2ECC71', fontSize: 14, fontWeight: 'bold', marginRight: 10 },
  cardCostPrice: { color: '#E74C3C', fontSize: 12 },
  quantityBadge: {
    backgroundColor: 'rgba(52, 152, 219, 0.2)',
    paddingVertical: 8,
    paddingHorizontal: 12,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#3498DB',
    marginLeft: 10,
  },
  quantityBadgeLow: {
    backgroundColor: 'rgba(231, 76, 60, 0.2)',
    borderColor: '#E74C3C',
  },
  quantityText: { color: '#ECF0F1', fontWeight: 'bold', fontSize: 14, textAlign: 'center' },
  emptyText: { color: '#7F8C8D', textAlign: 'center', marginTop: 50, fontSize: 16 }
});
