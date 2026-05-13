import paramiko

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
client.connect('5.182.26.100', username='root', password='5PMzEWGQl6O&od!r')

cmd = "sed -i 's/userId: req.user.id/userId: req.user.userId/g' /var/www/sotuvpos/saas-pos/backend/src/routes/debts.js; sed -i 's/userId: req.user.id/userId: req.user.userId/g' /var/www/sotuvpos/saas-pos/backend/src/routes/expenses.js; pm2 restart saas-pos-backend"

stdin, stdout, stderr = client.exec_command(cmd)
print("OUT:", stdout.read().decode())
print("ERR:", stderr.read().decode())
client.close()
