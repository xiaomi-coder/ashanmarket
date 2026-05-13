import React, { useState, useEffect } from 'react';
import { StyleSheet, Text, View, SafeAreaView, ScrollView, Dimensions, ActivityIndicator } from 'react-native';
import { StatusBar } from 'expo-status-bar';
import { PieChart, BarChart } from 'react-native-chart-kit';
import { LinearGradient } from 'expo-linear-gradient';
import api from '../config/api';

const screenWidth = Dimensions.get('window').width;

export default function ReportsScreen() {
  const [loading, setLoading] = useState(true);
  const [todayReport, setTodayReport] = useState({ totalRevenue: 0, totalProfit: 0 });
  const [monthReport, setMonthReport] = useState({ totalRevenue: 0, totalProfit: 0 });
  const [topProducts, setTopProducts] = useState([]);

  useEffect(() => {
    fetchReports();
  }, []);

  const fetchReports = async () => {
    try {
      const [todayRes, monthRes] = await Promise.all([
        api.get('/sales/web/report?range=today'),
        api.get('/sales/web/report?range=month')
      ]);
      setTodayReport(todayRes.data);
      setMonthReport(monthRes.data);
      setTopProducts(monthRes.data.topProducts || []);
    } catch (error) {
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  const pieData = topProducts.slice(0, 4).map((p, i) => {
    const colors = ['#3498DB', '#2ECC71', '#F1C40F', '#E74C3C'];
    return {
      name: p.productName.substring(0, 10),
      population: p.quantitySold,
      color: colors[i % colors.length],
      legendFontColor: '#BDC3C7',
      legendFontSize: 12
    };
  });

  // Dummy fallback if no products
  if (pieData.length === 0) {
    pieData.push({ name: 'Sotuv yo\'q', population: 1, color: '#95a5a6', legendFontColor: '#BDC3C7', legendFontSize: 12 });
  }

  const barData = {
    labels: ['Dush', 'Sesh', 'Chor', 'Pay', 'Jum', 'Shan', 'Yak'],
    datasets: [
      {
        data: [0, 0, 0, 0, 0, 0, 0],
      },
    ],
  };

  const chartConfig = {
    backgroundGradientFrom: '#2C3240',
    backgroundGradientTo: '#2C3240',
    color: (opacity = 1) => `rgba(52, 152, 219, ${opacity})`,
    labelColor: (opacity = 1) => `rgba(189, 195, 199, ${opacity})`,
    strokeWidth: 2,
    barPercentage: 0.5,
    useShadowColorFromDataset: false,
    decimalPlaces: 0,
  };

  if (loading) {
    return (
      <SafeAreaView style={[styles.container, { justifyContent: 'center', alignItems: 'center' }]}>
        <ActivityIndicator size="large" color="#3498DB" />
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.container}>
      <StatusBar style="light" />
      <View style={styles.header}>
        <Text style={styles.title}>Statistika va Hisobotlar</Text>
      </View>

      <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
        
        {/* Main Stats Row */}
        <View style={styles.statsRow}>
          <LinearGradient colors={['#3498DB', '#2980B9']} style={styles.statBox}>
            <Text style={styles.statLabel}>Bugungi savdo</Text>
            <Text style={styles.statValue}>{todayReport.totalRevenue.toLocaleString('ru-RU')}</Text>
            <Text style={styles.statProfit}>Sof foyda: {todayReport.totalProfit.toLocaleString('ru-RU')}</Text>
          </LinearGradient>
          
          <LinearGradient colors={['#2ECC71', '#27AE60']} style={styles.statBox}>
            <Text style={styles.statLabel}>Shu oylik savdo</Text>
            <Text style={styles.statValue}>{monthReport.totalRevenue.toLocaleString('ru-RU')}</Text>
            <Text style={styles.statProfit}>Sof foyda: {monthReport.totalProfit.toLocaleString('ru-RU')}</Text>
          </LinearGradient>
        </View>

        {/* Pie Chart Section */}
        <View style={styles.chartContainer}>
          <Text style={styles.chartTitle}>Kategoriyalar bo'yicha ulush</Text>
          <PieChart
            data={pieData}
            width={screenWidth - 40}
            height={200}
            chartConfig={chartConfig}
            accessor={"population"}
            backgroundColor={"transparent"}
            paddingLeft={"15"}
            absolute
          />
        </View>

        {/* Bar Chart Section */}
        <View style={styles.chartContainer}>
          <Text style={styles.chartTitle}>Haftalik savdo grafigi (mln so'm)</Text>
          <BarChart
            style={styles.barChart}
            data={barData}
            width={screenWidth - 40}
            height={220}
            yAxisLabel=""
            chartConfig={chartConfig}
            verticalLabelRotation={0}
            showValuesOnTopOfBars={true}
          />
        </View>
        
        {/* Extra spacing for bottom tabs */}
        <View style={{ height: 100 }} />
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#1A1E29' },
  header: { padding: 20, paddingTop: 50, paddingBottom: 10 },
  title: { color: 'white', fontSize: 24, fontWeight: 'bold' },
  content: { paddingHorizontal: 20 },
  statsRow: { flexDirection: 'row', justifyContent: 'space-between', marginBottom: 20, marginTop: 10 },
  statBox: { 
    flex: 1, 
    borderRadius: 16, 
    padding: 20, 
    marginHorizontal: 5,
    elevation: 5,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.3,
    shadowRadius: 5,
  },
  statLabel: { color: 'rgba(255,255,255,0.8)', fontSize: 13, fontWeight: '600' },
  statValue: { color: 'white', fontSize: 22, fontWeight: 'bold', marginTop: 8, marginBottom: 2 },
  statProfit: { color: '#F1C40F', fontSize: 12, fontWeight: 'bold', marginBottom: 8 },
  statTrend: { color: '#FFF', fontSize: 12, fontWeight: 'bold' },
  chartContainer: {
    backgroundColor: '#242A38',
    borderRadius: 20,
    padding: 15,
    marginBottom: 20,
    alignItems: 'center',
  },
  chartTitle: { color: '#ECF0F1', fontSize: 16, fontWeight: 'bold', alignSelf: 'flex-start', marginBottom: 15, marginLeft: 10 },
  barChart: { borderRadius: 16 },
});
