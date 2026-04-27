# 🛒 SaaS POS — To'liq Qo'llanma

## 📁 Loyiha Tuzilmasi

```
saas-pos/
├── backend/          ← Node.js + Express + Prisma
│   ├── src/
│   │   ├── index.js            # Server
│   │   ├── seed.js             # DB boshlang'ich ma'lumot
│   │   ├── routes/
│   │   │   ├── auth.js         # Login (tenant + super admin)
│   │   │   ├── superadmin.js   # Do'kon boshqaruvi
│   │   │   ├── products.js     # Mahsulotlar (web + EXE sync)
│   │   │   └── sales.js        # Sotuvlar (web + EXE sync)
│   │   ├── middleware/
│   │   │   └── auth.js         # JWT + ApiKey middleware
│   │   └── lib/prisma.js
│   └── prisma/schema.prisma    # DB schema
│
└── frontend/         ← React + Vite
    └── src/
        ├── pages/
        │   ├── Login.jsx        # Tenant kirish
        │   ├── SuperLogin.jsx   # Super admin kirish
        │   ├── SuperAdmin.jsx   # 🔐 Siz (barcha do'konlar)
        │   ├── Dashboard.jsx    # 📊 Do'kon overview
        │   ├── Cashier.jsx      # 🏪 Kassir ekrani
        │   ├── Products.jsx     # 📦 Mahsulotlar
        │   └── Reports.jsx      # 📈 Hisobotlar
        └── components/
            └── Layout.jsx       # Sidebar + navigation
```

---

## 🚀 Railway ga Deploy (Backend)

### 1. Railway da yangi loyiha

```
railway.app → New Project → Empty Project
```

### 2. PostgreSQL qo'shish

```
+ New → Database → Add PostgreSQL
```

### 3. Backend service qo'shish

```
+ New → GitHub Repo → backend papkasini tanlang
```

### 4. Environment variables (Railway → Variables)

```env
DATABASE_URL          = (Railway avtomatik beradi PostgreSQL dan)
JWT_SECRET            = supersecretkey123changethis
SUPER_ADMIN_USERNAME  = superadmin
SUPER_ADMIN_PASSWORD  = sizning_parolingiz
FRONTEND_URL          = https://sizning-frontend.vercel.app
PORT                  = 3000
```

### 5. Deploy settings

```
Root Directory: backend
Start Command: node src/index.js
```

### 6. Database migrate + seed

Railway terminal → Run:
```bash
npx prisma db push
node src/seed.js
```

---

## 🌐 Vercel ga Deploy (Frontend)

### 1. Vercel da yangi loyiha

```
vercel.com → New Project → GitHub Repo → frontend papka
```

### 2. Environment variables

```env
VITE_API_URL = https://sizning-railway-url.railway.app
```

### 3. Build settings

```
Framework: Vite
Root Directory: frontend
Build Command: npm run build
Output Directory: dist
```

---

## 💻 Local Test (MacBook)

```bash
# 1. Backend
cd backend
cp .env.example .env
# .env faylga DATABASE_URL ni kiriting (Railway PostgreSQL URL)
npm install
npx prisma db push
node src/seed.js
npm run dev
# → http://localhost:3000

# 2. Frontend (yangi terminal)
cd frontend
cp .env.example .env
# .env da VITE_API_URL=http://localhost:3000
npm install
npm run dev
# → http://localhost:5173
```

---

## 🔐 Login Ma'lumotlari

| Sahifa | URL | Login |
|--------|-----|-------|
| Super Admin | `/super-login` | seed.js da sozlangan |
| Test do'kon | `/login` | slug: `test-dokon`, `admin`/`admin123` |
| Test kassir | `/login` | slug: `test-dokon`, `kassir`/`kassir123` |

---

## 📋 Asosiy URL lar

```
/login          → Tenant (do'kon) kirish
/cashier        → 🏪 Kassir ekrani (barcode, sotuv)
/dashboard      → 📊 Bugungi statistika
/products       → 📦 Mahsulot boshqaruvi
/reports        → 📈 Hisobotlar (kun/hafta/oy)
/super-login    → 🔐 Siz (super admin)
/super          → Barcha do'konlar, obuna boshqaruvi
```

---

## 💡 SaaS Biznes Model

### Super Admin qilishi mumkin:
- ✅ Yangi do'kon yaratish (nom, slug, admin login/parol)
- ✅ Obuna muddatini uzaytirish (1/2/3/6/12 oy)
- ✅ Do'konni bloklash / aktiv qilish
- ✅ Barcha do'konlarning bugungi savdosini ko'rish
- ✅ 7 kun ichida tugaydigan obunalar haqida ogohlantirish

### Mijoz (do'kon egasi) ko'radi:
- ✅ Faqat o'z do'konining ma'lumotlari
- ✅ Kassir ekrani (barcode scan)
- ✅ Mahsulot qo'shish/tahrirlash
- ✅ Kunlik/haftalik/oylik hisobotlar
- ✅ Obuna qancha qolganini ko'radi

### EXE (Windows kassa) bilan ishlash:
- EXE API Key orqali cloud ga ulanadi: `x-api-key: tenant-api-key`
- `GET /api/products/sync` — mahsulotlarni yuklab olish
- `POST /api/sales/sync` — oflayn yig'ilgan sotuvlarni yuborish
- Internet yo'q bo'lsa — lokal SQLite da saqlanadi, qaytgach sync bo'ladi

---

## 📦 API Endpointlar

### Auth
```
POST /api/auth/login        → Tenant user login
POST /api/auth/super-login  → Super admin login
```

### Super Admin (JWT superadmin)
```
GET    /api/super/tenants           → Barcha do'konlar
POST   /api/super/tenants           → Yangi do'kon yaratish
PATCH  /api/super/tenants/:id       → Tahrirlash/bloklash/uzaytirish
GET    /api/super/stats             → Umumiy statistika
```

### Products (JWT tenant)
```
GET    /api/products/web            → Ro'yxat (search qo'llab-quvvatlanadi)
GET    /api/products/web/barcode/:b → Barcode bo'yicha
POST   /api/products/web            → Qo'shish (admin only)
PUT    /api/products/web/:id        → Tahrirlash (admin only)
DELETE /api/products/web/:id        → O'chirish (admin only)
```

### Sales (JWT tenant)
```
POST /api/sales/web         → Yangi sotuv
GET  /api/sales/web         → Tarix (admin only)
GET  /api/sales/web/report  → Hisobot ?from=&to= (admin only)
```

### EXE Sync (API Key)
```
GET  /api/products/sync             → Mahsulotlar + do'kon nomi/logo
GET  /api/products/sync/barcode/:b  → Barcode scan
POST /api/sales/sync                → Oflayn sotuvlarni yuklash
```

---

## 💰 Narxlash Tavsiyasi

| Tarif | Narx | Muddat |
|-------|------|--------|
| Oylik | 50,000 so'm | 1 oy |
| 3 oylik | 120,000 so'm | 3 oy (-20%) |
| Yillik | 400,000 so'm | 12 oy (-33%) |

**Xarajat:** Railway $5/oy → Sof foyda ≈ 100% boshida! 🔥

---

*SaaS POS — O'zbekiston supermarketlari uchun bulutli kassa tizimi*
