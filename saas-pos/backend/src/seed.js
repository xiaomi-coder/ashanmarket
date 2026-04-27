import bcrypt from 'bcryptjs'
import prisma from './lib/prisma.js'
import dotenv from 'dotenv'
dotenv.config()

async function main() {
  console.log('🌱 Seeding...')

  // Super Admin
  const existing = await prisma.superAdmin.findUnique({
    where: { username: process.env.SUPER_ADMIN_USERNAME || 'superadmin' }
  })

  if (!existing) {
    await prisma.superAdmin.create({
      data: {
        username: process.env.SUPER_ADMIN_USERNAME || 'superadmin',
        passwordHash: await bcrypt.hash(process.env.SUPER_ADMIN_PASSWORD || 'changeme123', 10)
      }
    })
    console.log(`✅ Super Admin yaratildi: ${process.env.SUPER_ADMIN_USERNAME || 'superadmin'}`)
  } else {
    console.log('ℹ️  Super Admin allaqachon mavjud')
  }

  // Test tenant (ixtiyoriy)
  const testTenant = await prisma.tenant.findUnique({ where: { slug: 'test-dokon' } })
  if (!testTenant) {
    const expires = new Date()
    expires.setMonth(expires.getMonth() + 1)

    await prisma.tenant.create({
      data: {
        name: "Test Do'kon",
        slug: 'test-dokon',
        phone: '+998901234567',
        expiresAt: expires,
        categories: {
          create: [
            { name: 'Oziq-ovqat', color: '#4CAF50' },
            { name: 'Ichimliklar', color: '#2196F3' },
            { name: 'Boshqalar', color: '#607D8B' },
          ]
        },
        users: {
          create: [
            {
              username: 'admin',
              passwordHash: await bcrypt.hash('admin123', 10),
              fullName: 'Test Admin',
              role: 'admin'
            },
            {
              username: 'kassir',
              passwordHash: await bcrypt.hash('kassir123', 10),
              fullName: 'Test Kassir',
              role: 'cashier'
            }
          ]
        }
      }
    })
    console.log("✅ Test do'kon yaratildi: slug=test-dokon, admin/admin123")
  }

  console.log('✅ Seed tugadi!')
  await prisma.$disconnect()
}

main().catch(e => { console.error(e); process.exit(1) })
