import React from 'react';
import { StyleSheet, Text, View, SafeAreaView, ScrollView, Dimensions } from 'react-native';
import { StatusBar } from 'expo-status-bar';
import { PieChart, BarChart } from 'react-native-chart-kit';
import { LinearGradient } from 'expo-linear-gradient';

const screenWidth = Dimensions.get('window').width;

export default function ReportsScreen() {
  const pieData = [
    { name: 'Oziq-ovqat', population: 45, color: '#3498DB', legendFontColor: '#BDC3C7', legendFontSize: 12 },
    { name: 'Ichimliklar', population: 25, color: '#2ECC71', legendFontColor: '#BDC3C7', legendFontSize: 12 },
    { name: 'Shirinliklar', population: 20, color: '#F1C40F', legendFontColor: '#BDC3C7', legendFontSize: 12 },
    { name: 'Boshqa', population: 10, color: '#E74C3C', legendFontColor: '#BDC3C7', legendFontSize: 12 },
  ];

  const barData = {
    labels: ['Dush', 'Sesh', 'Chor', 'Pay', 'Jum', 'Shan', 'Yak'],
    datasets: [
      {
        data: [20, 45, 28, 80, 99, 43, 50],
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
            <Text style={styles.statValue}>4 500 000</Text>
            <Text style={styles.statProfit}>Sof foyda: 1 200 000</Text>
            <Text style={styles.statTrend}>↑ +15%</Text>
          </LinearGradient>
          
          <LinearGradient colors={['#2ECC71', '#27AE60']} style={styles.statBox}>
            <Text style={styles.statLabel}>Shu oylik savdo</Text>
            <Text style={styles.statValue}>124 500 000</Text>
            <Text style={styles.statProfit}>Sof foyda: 25 300 000</Text>
            <Text style={styles.statTrend}>↑ +8%</Text>
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
