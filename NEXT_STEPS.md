# SupermarketPOS va SaaS Web Platforma - Ertangi Vazifalar (Next Steps)

Ushbu hujjat keyingi sessiyada ishni aynan qayerdan boshlashimizni eslatib turish uchun saqlandi. Dastur kodi 100% yozib bo'lingan (Kassa, Vozvrat, Xarajatlar, Xodimlar, Sinxronizatsiya va Web Dashboard).

## Ertangi asosiy qadamlar:

### 1. Web Platformani Serverga Joylash (Deployment)
- Node.js backend (`saas-pos/backend`) ni Linux VPS serverda (PM2 orqali) ishga tushirish.
- React frontend (`saas-pos/frontend`) ni serverga joylash.
- Nginx o'rnatish va uni teskari proksi (Reverse Proxy) sifatida sozlash.

### 2. Domen Ulash (Domain Setup)
- Olingan `.uz` domenni (masalan, `kassam.uz`) VPS IP manziliga DNS orqali ulash.
- SSL sertifikat (Let's Encrypt / Certbot) o'rnatish (HTTPS ishlashi uchun).

### 3. WPF Dasturini Serverga Bog'lash
- WPF kodidagi (`SyncService` dagi) vaqtincha yozilgan `http://localhost:5000` URL'ni yangi haqiqiy domen URL iga almashtirish.
- *Qo'shimcha tavsiya:* URL va API Key ni kod ichiga qattiq yozmasdan, `Settings` (Sozlamalar) bo'limidan kiritiladigan qilish (shunda mijoz o'zi xohlagan serverga ulay oladi).

### 4. Yakuniy '.exe' Yig'ish (Publish)
- Dasturni `dotnet publish` orqali Release formatida, hech qanday framework'siz (Self-contained) bitta `.exe` qilib yig'ib olish.

### 5. Final Test
- `.exe` dasturni ishga tushirib savdo qilish va "Sinxronizatsiya" tugmasini bosib, Web Dashboard orqali sotuvlarni onlayn kuzatish. 

> *Eslatma AI uchun:* Keyingi marta foydalanuvchi suhbatni boshlaganda, shu faylni (NEXT_STEPS.md) o'qi va bevosita Serverga joylash/Domain ulash bosqichidan davom et!
