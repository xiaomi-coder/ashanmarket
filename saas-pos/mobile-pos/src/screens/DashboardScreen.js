import React from 'react';
import { StyleSheet, Text, View, ScrollView, TouchableOpacity, SafeAreaView, Dimensions, TextInput, Image, Platform } from 'react-native';
import { StatusBar } from 'expo-status-bar';
import { LinearGradient } from 'expo-linear-gradient';
import { Ionicons, MaterialCommunityIcons, Feather } from '@expo/vector-icons';

const { width } = Dimensions.get('window');
const itemWidth = (width - 50) / 2;

export default function DashboardScreen({ navigation }) {
  const transactions = [
    { id: '1', name: 'Alisher', time: '14:32', items: 7, total: '185,000', icon: '👤' },
    { id: '2', name: 'Malika', time: '14:15', items: 3, total: '72,500', icon: '👩' },
    { id: '3', name: 'Nodir', time: '13:05', items: 12, total: '340,000', icon: '👨' },
  ];

  return (
    <SafeAreaView style={styles.container}>
      <StatusBar style="light" />
      
      {/* Top Header */}
      <View style={styles.header}>
        <View style={styles.searchBar}>
          <Feather name="search" size={18} color="#7F8C8D" />
          <TextInput 
            style={styles.searchInput} 
            placeholder="Qidiruv" 
            placeholderTextColor="#7F8C8D"
          />
        </View>
        <View style={styles.headerIcons}>
          <TouchableOpacity style={styles.iconBtn}>
            <Ionicons name="person-circle-outline" size={28} color="#BDC3C7" />
          </TouchableOpacity>
          <TouchableOpacity style={styles.iconBtn}>
            <Ionicons name="notifications-outline" size={24} color="#BDC3C7" />
            <View style={styles.badge} />
          </TouchableOpacity>
        </View>
      </View>

      <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
        
        <View style={styles.titleRow}>
          <Ionicons name="cart" size={28} color="#3498DB" />
          <Text style={styles.mainTitle}>ASHAN MARKET</Text>
        </View>

        {/* Total Sales Card */}
        <LinearGradient colors={['#2980B9', '#1A5276']} start={{x: 0, y: 0}} end={{x: 1, y: 1}} style={styles.mainCard}>
          <View style={styles.cardTopRow}>
            <Text style={styles.cardLabel}>JAMI SAVDO (Bugun)</Text>
            <Text style={styles.cardTrend}>↗ +15.2%</Text>
          </View>
          <Text style={styles.cardValue}>4 500 000 so'm</Text>
          <Text style={styles.cardProfit}>Jami foyda: 1 200 000 so'm</Text>
          
          {/* Decorative background shapes for gradient */}
          <View style={styles.glassReflection} />
        </LinearGradient>

        {/* Action Buttons Grid */}
        <View style={styles.grid}>
          <TouchableOpacity style={styles.gridItem} onPress={() => navigation.navigate('Scanner')}>
            <LinearGradient colors={['rgba(255,255,255,0.1)', 'rgba(255,255,255,0.05)']} style={styles.gridInner}>
              <MaterialCommunityIcons name="qrcode-scan" size={40} color="#3498DB" style={styles.gridIcon} />
              <Text style={styles.gridItemText}>SKANER</Text>
            </LinearGradient>
          </TouchableOpacity>

          <TouchableOpacity style={styles.gridItem} onPress={() => navigation.navigate('Expenses')}>
            <LinearGradient colors={['rgba(255,255,255,0.1)', 'rgba(255,255,255,0.05)']} style={styles.gridInner}>
              <MaterialCommunityIcons name="sack-percent" size={40} color="#9B59B6" style={styles.gridIcon} />
              <Text style={styles.gridItemText}>XARAJATLAR</Text>
            </LinearGradient>
          </TouchableOpacity>

          <TouchableOpacity style={styles.gridItem} onPress={() => navigation.navigate('Debts')}>
            <LinearGradient colors={['rgba(255,255,255,0.1)', 'rgba(255,255,255,0.05)']} style={styles.gridInner}>
              <MaterialCommunityIcons name="account-cash-outline" size={40} color="#E74C3C" style={styles.gridIcon} />
              <Text style={styles.gridItemText}>QARZLAR</Text>
            </LinearGradient>
          </TouchableOpacity>

          <TouchableOpacity style={styles.gridItem} onPress={() => navigation.navigate('Warehouse')}>
            <LinearGradient colors={['rgba(255,255,255,0.1)', 'rgba(255,255,255,0.05)']} style={styles.gridInner}>
              <MaterialCommunityIcons name="forklift" size={40} color="#2ECC71" style={styles.gridIcon} />
              <Text style={styles.gridItemText}>OMBOR</Text>
            </LinearGradient>
          </TouchableOpacity>
        </View>

        {/* Recent Transactions */}
        <Text style={styles.sectionTitle}>So'nggi Sotuvlar</Text>
        <View style={styles.transactionsList}>
          {transactions.map((tx) => (
            <View key={tx.id} style={styles.txRow}>
              <View style={styles.txAvatar}>
                <Text style={styles.txAvatarIcon}>{tx.icon}</Text>
              </View>
              <View style={styles.txDetails}>
                <Text style={styles.txLabel}>Mijoz</Text>
                <Text style={styles.txName}>{tx.name}</Text>
              </View>
              <View style={styles.txDetailsMid}>
                <Text style={styles.txLabel}>Vaqt</Text>
                <Text style={styles.txValue}>{tx.time}</Text>
              </View>
              <View style={styles.txDetailsMid}>
                <Text style={styles.txLabel}>Soni</Text>
                <Text style={styles.txValue}>{tx.items}</Text>
              </View>
              <View style={styles.txDetailsRight}>
                <Text style={styles.txLabel}>Summa</Text>
                <Text style={styles.txTotal}>{tx.total} so'm</Text>
              </View>
            </View>
          ))}
        </View>

        <View style={{ height: 100 }} />
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#161925',
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 20,
    paddingTop: Platform.OS === 'android' ? 40 : 20,
    marginBottom: 20,
  },
  searchBar: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#242A38',
    borderRadius: 12,
    paddingHorizontal: 12,
    height: 40,
    marginRight: 15,
  },
  searchInput: {
    flex: 1,
    color: 'white',
    marginLeft: 8,
    fontSize: 14,
  },
  headerIcons: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  iconBtn: {
    marginLeft: 15,
    position: 'relative',
  },
  badge: {
    position: 'absolute',
    top: 2,
    right: 2,
    width: 8,
    height: 8,
    backgroundColor: '#E74C3C',
    borderRadius: 4,
  },
  content: {
    flex: 1,
    paddingHorizontal: 20,
  },
  titleRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 20,
  },
  mainTitle: {
    color: '#ECF0F1',
    fontSize: 22,
    fontWeight: 'bold',
    marginLeft: 10,
    letterSpacing: 1,
  },
  mainCard: {
    borderRadius: 24,
    padding: 24,
    marginBottom: 25,
    overflow: 'hidden',
    elevation: 10,
    shadowColor: '#3498DB',
    shadowOffset: { width: 0, height: 10 },
    shadowOpacity: 0.3,
    shadowRadius: 15,
  },
  glassReflection: {
    position: 'absolute',
    top: -50,
    right: -50,
    width: 150,
    height: 150,
    backgroundColor: 'rgba(255,255,255,0.1)',
    borderRadius: 75,
  },
  cardTopRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 15,
  },
  cardLabel: {
    color: 'rgba(255, 255, 255, 0.7)',
    fontSize: 12,
    fontWeight: 'bold',
    letterSpacing: 0.5,
  },
  cardTrend: {
    color: '#2ECC71',
    fontSize: 12,
    fontWeight: 'bold',
  },
  cardValue: {
    color: '#FFF',
    fontSize: 32,
    fontWeight: 'bold',
  },
  cardProfit: {
    color: '#F1C40F',
    fontSize: 14,
    fontWeight: 'bold',
    marginTop: 5,
  },
  grid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    justifyContent: 'space-between',
    marginBottom: 30,
  },
  gridItem: {
    width: itemWidth,
    height: itemWidth * 0.8,
    marginBottom: 15,
    borderRadius: 20,
    overflow: 'hidden',
  },
  gridInner: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    borderWidth: 1,
    borderColor: 'rgba(255,255,255,0.1)',
    borderRadius: 20,
  },
  gridIcon: {
    marginBottom: 10,
  },
  gridItemText: {
    color: '#ECF0F1',
    fontSize: 12,
    fontWeight: '600',
    letterSpacing: 0.5,
  },
  sectionTitle: {
    color: '#ECF0F1',
    fontSize: 16,
    fontWeight: 'bold',
    marginBottom: 15,
  },
  transactionsList: {
    marginBottom: 20,
  },
  txRow: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#1E2330',
    padding: 15,
    borderRadius: 16,
    marginBottom: 10,
  },
  txAvatar: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: '#2C3240',
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 15,
  },
  txAvatarIcon: { fontSize: 20 },
  txDetails: { flex: 2 },
  txDetailsMid: { flex: 1, alignItems: 'center' },
  txDetailsRight: { flex: 2, alignItems: 'flex-end' },
  txLabel: { color: '#7F8C8D', fontSize: 10, marginBottom: 2 },
  txName: { color: '#ECF0F1', fontSize: 13, fontWeight: 'bold' },
  txValue: { color: '#BDC3C7', fontSize: 13 },
  txTotal: { color: '#ECF0F1', fontSize: 13, fontWeight: 'bold' },
});
