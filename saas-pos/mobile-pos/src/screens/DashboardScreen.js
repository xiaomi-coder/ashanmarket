import React from 'react';
import { StyleSheet, Text, View, ScrollView, TouchableOpacity, SafeAreaView } from 'react-native';
import { StatusBar } from 'expo-status-bar';

export default function DashboardScreen({ navigation }) {
  return (
    <SafeAreaView style={styles.container}>
      <StatusBar style="light" />
      
      {/* Header */}
      <View style={styles.header}>
        <View>
          <Text style={styles.headerSubtitle}>Bugun, 12 May 2026</Text>
          <Text style={styles.headerTitle}>Xush kelibsiz, Admin</Text>
        </View>
        <View style={styles.avatar}>
          <Text style={styles.avatarText}>A</Text>
        </View>
      </View>

      <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
        
        {/* Main Stats Card */}
        <View style={styles.mainCard}>
          <Text style={styles.cardLabel}>Bugungi jami savdo</Text>
          <Text style={styles.cardValue}>4 500 000 so'm</Text>
          <View style={styles.cardFooter}>
            <Text style={styles.cardTrend}>+12.5% kechagiga nisbatan</Text>
          </View>
        </View>

        {/* Quick Stats Grid */}
        <View style={styles.grid}>
          <View style={[styles.gridItem, { backgroundColor: '#2C3240' }]}>
            <Text style={styles.gridItemLabel}>Cheklar soni</Text>
            <Text style={styles.gridItemValue}>142 ta</Text>
          </View>
          <View style={[styles.gridItem, { backgroundColor: '#2C3240' }]}>
            <Text style={styles.gridItemLabel}>O'rtacha chek</Text>
            <Text style={styles.gridItemValue}>31 690 so'm</Text>
          </View>
        </View>

        {/* Action Buttons */}
        <Text style={styles.sectionTitle}>Tezkor amallar</Text>
        <View style={styles.actionGrid}>
          
          <TouchableOpacity style={styles.actionBtn} onPress={() => navigation.navigate('Scanner')}>
            <Text style={styles.actionIcon}>📷</Text>
            <Text style={styles.actionText}>Skaner</Text>
          </TouchableOpacity>

          <TouchableOpacity style={styles.actionBtn}>
            <Text style={styles.actionIcon}>💸</Text>
            <Text style={styles.actionText}>Xarajat</Text>
          </TouchableOpacity>

          <TouchableOpacity style={styles.actionBtn}>
            <Text style={styles.actionIcon}>📒</Text>
            <Text style={styles.actionText}>Qarzlar</Text>
          </TouchableOpacity>

          <TouchableOpacity style={styles.actionBtn}>
            <Text style={styles.actionIcon}>📦</Text>
            <Text style={styles.actionText}>Ombor</Text>
          </TouchableOpacity>

        </View>

      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#1E232E',
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: 24,
    paddingTop: 40,
    paddingBottom: 20,
    backgroundColor: '#1E232E',
  },
  headerSubtitle: {
    color: '#7F8C8D',
    fontSize: 14,
    marginBottom: 4,
  },
  headerTitle: {
    color: '#ECF0F1',
    fontSize: 22,
    fontWeight: 'bold',
  },
  avatar: {
    width: 44,
    height: 44,
    borderRadius: 22,
    backgroundColor: '#3498DB',
    alignItems: 'center',
    justifyContent: 'center',
  },
  avatarText: {
    color: '#FFF',
    fontSize: 20,
    fontWeight: 'bold',
  },
  content: {
    flex: 1,
    paddingHorizontal: 20,
  },
  mainCard: {
    backgroundColor: '#3498DB',
    borderRadius: 20,
    padding: 24,
    marginTop: 10,
    marginBottom: 20,
    elevation: 6,
    shadowColor: '#3498DB',
    shadowOffset: { width: 0, height: 6 },
    shadowOpacity: 0.3,
    shadowRadius: 10,
  },
  cardLabel: {
    color: 'rgba(255, 255, 255, 0.8)',
    fontSize: 15,
    fontWeight: '500',
  },
  cardValue: {
    color: '#FFF',
    fontSize: 32,
    fontWeight: 'bold',
    marginVertical: 10,
  },
  cardFooter: {
    backgroundColor: 'rgba(255, 255, 255, 0.2)',
    alignSelf: 'flex-start',
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 20,
  },
  cardTrend: {
    color: '#FFF',
    fontSize: 12,
    fontWeight: 'bold',
  },
  grid: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 30,
  },
  gridItem: {
    width: '48%',
    borderRadius: 16,
    padding: 16,
  },
  gridItemLabel: {
    color: '#7F8C8D',
    fontSize: 14,
    marginBottom: 8,
  },
  gridItemValue: {
    color: '#ECF0F1',
    fontSize: 20,
    fontWeight: 'bold',
  },
  sectionTitle: {
    color: '#ECF0F1',
    fontSize: 18,
    fontWeight: 'bold',
    marginBottom: 16,
  },
  actionGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    justifyContent: 'space-between',
  },
  actionBtn: {
    width: '48%',
    backgroundColor: '#2C3240',
    borderRadius: 16,
    padding: 20,
    alignItems: 'center',
    marginBottom: 15,
  },
  actionIcon: {
    fontSize: 32,
    marginBottom: 10,
  },
  actionText: {
    color: '#BDC3C7',
    fontSize: 14,
    fontWeight: '600',
  },
});
