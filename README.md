# 🛒 SupermarketPOS — Production-Ready Offline POS System

> **Windows (.exe) | C# WPF | .NET 8 | SQLite | MVVM | Offline-First**

---

## 📁 Project Structure

```
SupermarketPOS/
├── SupermarketPOS.csproj          # Project file (NuGet packages)
├── App.xaml                       # Application entry + DI setup
├── App.xaml.cs                    # DI container, startup logic
│
├── Models/
│   ├── Product.cs                 # Product entity
│   └── Sale.cs                    # Sale, SaleItem, User, Category, Reports
│
├── Data/
│   ├── DatabaseContext.cs         # SQLite connection + schema creation
│   └── DatabaseSeeder.cs          # 30 sample products + 2 users
│
├── Repositories/
│   ├── ProductRepository.cs       # IProductRepository + implementation
│   ├── SaleRepository.cs          # ISaleRepository + implementation
│   └── UserRepository.cs          # IUserRepository + implementation
│
├── Services/
│   └── Services.cs                # IAuthService, IProductService, ISaleService
│
├── ViewModels/
│   ├── BaseViewModel.cs           # INotifyPropertyChanged base
│   ├── SalesViewModel.cs          # Main POS logic + CartItem
│   ├── ProductManagementViewModel.cs
│   └── OtherViewModels.cs         # ReportsViewModel, LoginViewModel, MainViewModel
│
├── Views/
│   ├── LoginWindow.xaml/cs        # Login screen
│   ├── MainWindow.xaml/cs         # Shell with sidebar navigation
│   ├── SalesView.xaml/cs          # 🏪 Kassir screen (main)
│   ├── ProductManagementView.xaml/cs
│   └── ReportsView.xaml/cs        # 📊 Reports screen
│
├── Helpers/
│   ├── RelayCommand.cs            # ICommand implementations
│   └── ReceiptGenerator.cs        # Text receipt generator
│
├── Converters/
│   └── Converters.cs              # WPF value converters
│
├── Themes/
│   ├── DarkTheme.xaml             # Color palette
│   └── Styles.xaml                # Button, TextBox, ListView styles
│
└── Assets/
    └── sample_import.csv          # Sample CSV for bulk import
```

---

## 🚀 Quick Start (5 daqiqa)

### 1. Talab qilinadigan vositalar

| Vosita | Versiya | Link |
|--------|---------|------|
| .NET SDK | 8.0+ | https://dotnet.microsoft.com/download |
| Visual Studio | 2022+ | https://visualstudio.microsoft.com |
| Windows | 10/11 | — |

### 2. Loyihani yuklab olish va ishga tushirish

```bash
# 1. Loyiha papkasiga o'tish
cd SupermarketPOS

# 2. Paketlarni o'rnatish
dotnet restore

# 3. Ishga tushirish (debug)
dotnet run

# 4. Release build (EXE yaratish)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
# EXE fayl: bin/Release/net8.0-win-x64/publish/SupermarketPOS.exe
```

### 3. Visual Studio orqali

1. `SupermarketPOS.csproj` faylini VS 2022 da oching
2. `F5` yoki `Ctrl+F5` bosing
3. Login: `admin` / `admin123`

---

## 🔐 Default Login Credentials

| Foydalanuvchi | Parol | Rol |
|---------------|-------|-----|
| `admin` | `admin123` | Admin (barcha imkoniyatlar) |
| `kassir` | `kassir123` | Cashier (faqat sotuv) |

---

## 🎯 Features

### 🏪 Sotuv Ekrani (Kassir)
- **Barcode scanner** — klaviatura orqali barcode o'qish (Enter bilan)
- **Mahsulot qidirish** — nom yoki barcode bo'yicha
- **Savatcha** — miqdor o'zgartirish (+ / −), o'chirish
- **Real-time hisob** — jami, chegirma, qaytim
- **Tez summa tugmalari** — 10K, 20K, 50K, 100K
- **To'lov turlari** — Naqd / Karta / Transfer
- **Chek ko'rish** — matnli format (printer uchun tayyor)
- **F5** — sotuvni yakunlash

### 📦 Mahsulotlar (Admin)
- Qo'shish / Tahrirlash / O'chirish
- Barcode, nom, narx, tan narx, qoldiq
- Kategoriyalar
- CSV import (`Barcode,Name,Price,CostPrice,Stock,CategoryId,Unit`)
- Kam qoldiq ogohlantirish

### 📊 Hisobotlar (Admin)
- Bugun / Hafta / Oy filtri
- Jami tranzaksiyalar, tushum, foyda, chegirmalar
- Savdo tarixi ro'yxati
- Top-10 eng ko'p sotiladigan mahsulotlar

### 🔒 Xavfsizlik
- BCrypt parol hashlash
- Role-based access (Admin / Cashier)
- Kassir faqat sotuv sahifasini ko'radi

---

## ⌨️ Keyboard Shortcuts

| Tugma | Amal |
|-------|------|
| `F1` | Sotuv ekrani |
| `F2` | Mahsulotlar (Admin) |
| `F3` | Hisobotlar (Admin) |
| `F5` | Sotuvni yakunlash |
| `Enter` (barcode) | Shtrix-kodni qo'shish |

---

## 🗄️ Database

**Joylashuv:** `%LocalAppData%\SupermarketPOS\supermarket_pos.db`

Masalan: `C:\Users\YourName\AppData\Local\SupermarketPOS\supermarket_pos.db`

### Jadvallar

```sql
Categories  -- Kategoriyalar (10 ta)
Products    -- Mahsulotlar (30 ta namuna)
Users       -- Foydalanuvchilar (admin + kassir)
Sales       -- Sotuvlar
SaleItems   -- Sotuv qatorlari
```

### Optimizatsiyalar
- **WAL mode** — tezkor yozish
- **Indekslar** — barcode, nom, sana bo'yicha
- **Virtual ListView** — 10,000+ mahsulot ham tez ishlaydi
- **Async/await** — UI muzlamaydi
- **Connection pooling** — Dapper orqali

---

## 📥 CSV Import Formati

`Assets/sample_import.csv` faylidan namuna ko'ring:

```csv
Barcode,Name,Price,CostPrice,Stock,CategoryId,Unit
8690000010001,Yangi mahsulot,15000,10000,100,1,dona
```

**CategoryId:** 1=Oziq-ovqat, 2=Ichimliklar, 3=Sut, 4=Non, 5=Go'sht, 
6=Sabzavot, 7=Meva, 8=Kimyo, 9=Gigiyena, 10=Boshqa

---

## 💾 Zaxira (Backup)

Sidebar → "💾 Zaxira saqlash" tugmasi → Desktop/POS_Backup/ papkasiga `.db` fayl saqlanadi.

---

## 🔧 Konfiguratsiya

`App.xaml.cs` da sozlashingiz mumkin:

```csharp
// Do'kon nomi (chek uchun)
ReceiptGenerator.GenerateText(sale, "DO'KON NOMINGIZ");

// Ma'lumotlar bazasi fayl nomi
services.AddSingleton<DatabaseContext>(_ => new DatabaseContext("mening_db.db"));
```

---

## 🏗️ Architecture

```
View (XAML)
  ↕ DataBinding
ViewModel (INotifyPropertyChanged)
  ↕ Interface calls
Service Layer (business logic)
  ↕ Interface calls  
Repository Layer (data access)
  ↕ Dapper + SQL
SQLite Database
```

**Design Patterns:**
- MVVM (Model-View-ViewModel)
- Repository Pattern
- Service Layer Pattern
- Dependency Injection (Microsoft.Extensions.DI)
- Command Pattern (RelayCommand / AsyncRelayCommand)

---

## 🐛 Muammolarni hal qilish

| Muammo | Yechim |
|--------|--------|
| `dotnet` topilmadi | .NET 8 SDK o'rnating |
| Barcode ishlamayapti | Enter tugmasini bosing (scanner Enter yuborishi kerak) |
| Login ishlamayapti | `admin` / `admin123` ishlatib ko'ring |
| DB xatosi | `%LocalAppData%\SupermarketPOS\` papkasini tekshiring |
| UI muzladi | Async methods to'g'ri ishlayapti — kuting |

---

## 📦 NuGet Packages

```xml
Microsoft.Data.Sqlite   8.0.0   <!-- SQLite driver -->
Dapper                  2.1.28  <!-- Micro-ORM -->
Microsoft.Extensions.DependencyInjection 8.0.0
CsvHelper               33.0.1  <!-- CSV import -->
BCrypt.Net-Next         4.0.3   <!-- Password hashing -->
QRCoder                 1.4.3   <!-- (kelajak uchun) -->
```

---

## 🚀 Kelajak Rejalari

- [ ] QR kod chek (QRCoder paket tayyor)
- [ ] API sync (REST backend bilan)
- [ ] Receipt printer (ESCPOS)
- [ ] Shift management (smena hisoboti)
- [ ] Multi-terminal support
- [ ] Customer loyalty cards
- [ ] Barcode generator (yangi mahsulotlar uchun)

---

## 📄 Litsenziya

MIT License — erkin foydalanish mumkin.

---

*SupermarketPOS — O'zbekiston supermarketlari uchun maxsus ishlab chiqilgan.*
