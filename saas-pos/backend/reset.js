import bcrypt from 'bcryptjs';
import prisma from './src/lib/prisma.js';

async function reset() {
  const hash = await bcrypt.hash('changeme123', 10);
  await prisma.superAdmin.update({
    where: { username: 'superadmin' },
    data: { passwordHash: hash }
  });
  console.log('Password reset to changeme123');
  process.exit(0);
}

reset();
