import bcrypt from 'bcryptjs';
import prisma from './src/lib/prisma.js';
async function check() {
  const admin = await prisma.superAdmin.findUnique({where: {username: 'superadmin'}});
  console.log(admin);
  const valid = await bcrypt.compare('changeme123', admin.passwordHash);
  console.log('valid:', valid);
  process.exit(0);
}
check();
