import React from 'react';
import { StyleSheet, Text, View, SafeAreaView, TouchableOpacity, ScrollView, Alert } from 'react-native';
import { StatusBar } from 'expo-status-bar';
import { Ionicons, MaterialCommunityIcons, Feather } from '@expo/vector-icons';

export default function MenuScreen() {
  
  const handleLogout = () => {
    Alert.alert("Tizimdan chiqish", "Haqiqatan ham hisobdan chiqmoqchimisiz?", [
      { text: "Yo'q", style: "cancel" },
      { text: "Ha, chiqish", style: "destructive", onPress: () => console.log('Logged out') }
    ]);
  };

  const handleZReport = () => {
    Alert.alert("Z-Hisobot", "Kassani yopib, kunlik savdoni yakunlaysizmi?", [
      { text: "Bekor qilish", style: "cancel" },
      { text: "Yakunlash", onPress: () => console.log('Z Report generated') }
    ]);
  };

  return (
    <SafeAreaView style={styles.container}>
      <StatusBar style="light" />
      <View style={styles.header}>
        <Text style={styles.title}>Qo'shimcha Menu</Text>
      </View>

      <ScrollView style={styles.content}>
        
        <View style={styles.profileSection}>
          <View style={styles.avatar}>
            <Text style={styles.avatarText}>A</Text>
          </View>
          <View style={styles.profileInfo}>
            <Text style={styles.profileName}>Admin (Asosiy Kassa)</Text>
            <Text style={styles.profileRole}>Do'kon boshqaruvchisi</Text>
          </View>
        </View>

        <Text style={styles.sectionTitle}>Asosiy Amallar</Text>
        <View style={styles.menuGroup}>
          <TouchableOpacity style={styles.menuItem} onPress={handleZReport}>
            <MaterialCommunityIcons name="cash-register" size={24} color="#F1C40F" style={styles.menuIcon} />
            <Text style={styles.menuText}>Kassani yopish (Z-Hisobot)</Text>
            <Feather name="chevron-right" size={20} color="#7F8C8D" />
          </TouchableOpacity>
          <View style={styles.divider} />
          <TouchableOpacity style={styles.menuItem}>
            <Ionicons name="people" size={24} color="#3498DB" style={styles.menuIcon} />
            <Text style={styles.menuText}>Xodimlar va Kassirlar</Text>
            <Feather name="chevron-right" size={20} color="#7F8C8D" />
          </TouchableOpacity>
        </View>

        <Text style={styles.sectionTitle}>Tizim Sozlamalari</Text>
        <View style={styles.menuGroup}>
          <TouchableOpacity style={styles.menuItem}>
            <Ionicons name="print" size={24} color="#BDC3C7" style={styles.menuIcon} />
            <Text style={styles.menuText}>Printer sozlamalari</Text>
            <Feather name="chevron-right" size={20} color="#7F8C8D" />
          </TouchableOpacity>
          <View style={styles.divider} />
          <TouchableOpacity style={styles.menuItem}>
            <Ionicons name="settings" size={24} color="#BDC3C7" style={styles.menuIcon} />
            <Text style={styles.menuText}>Umumiy sozlamalar</Text>
            <Feather name="chevron-right" size={20} color="#7F8C8D" />
          </TouchableOpacity>
        </View>

        <TouchableOpacity style={styles.logoutButton} onPress={handleLogout}>
          <Ionicons name="log-out-outline" size={24} color="#E74C3C" style={{ marginRight: 10 }} />
          <Text style={styles.logoutText}>Tizimdan chiqish</Text>
        </TouchableOpacity>

      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#161925' },
  header: { padding: 20, paddingTop: 50, paddingBottom: 10 },
  title: { color: 'white', fontSize: 24, fontWeight: 'bold' },
  content: { flex: 1, paddingHorizontal: 20 },
  profileSection: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#242A38',
    padding: 20,
    borderRadius: 16,
    marginBottom: 30,
    marginTop: 10,
  },
  avatar: {
    width: 60,
    height: 60,
    borderRadius: 30,
    backgroundColor: '#3498DB',
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 15,
  },
  avatarText: { color: 'white', fontSize: 28, fontWeight: 'bold' },
  profileName: { color: 'white', fontSize: 18, fontWeight: 'bold', marginBottom: 5 },
  profileRole: { color: '#7F8C8D', fontSize: 14 },
  sectionTitle: { color: '#7F8C8D', fontSize: 14, fontWeight: 'bold', textTransform: 'uppercase', marginBottom: 10, marginLeft: 5 },
  menuGroup: {
    backgroundColor: '#242A38',
    borderRadius: 16,
    marginBottom: 25,
    overflow: 'hidden',
  },
  menuItem: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 18,
  },
  menuIcon: { width: 30, marginRight: 15 },
  menuText: { flex: 1, color: '#ECF0F1', fontSize: 16 },
  divider: { height: 1, backgroundColor: 'rgba(255,255,255,0.05)', marginLeft: 60 },
  logoutButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: 'rgba(231, 76, 60, 0.1)',
    padding: 16,
    borderRadius: 16,
    marginTop: 10,
    borderWidth: 1,
    borderColor: 'rgba(231, 76, 60, 0.3)',
  },
  logoutText: { color: '#E74C3C', fontSize: 16, fontWeight: 'bold' }
});
