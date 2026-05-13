const { Client } = require('ssh2');

const conn = new Client();
conn.on('ready', () => {
  console.log('Client :: ready');
  conn.exec(`sed -i 's/userId: req.user.id/userId: req.user.userId/g' /var/www/sotuvpos/saas-pos/backend/src/routes/debts.js && sed -i 's/userId: req.user.id/userId: req.user.userId/g' /var/www/sotuvpos/saas-pos/backend/src/routes/expenses.js && pm2 restart saas-pos-backend`, (err, stream) => {
    if (err) throw err;
    stream.on('close', (code, signal) => {
      console.log('Stream :: close :: code: ' + code + ', signal: ' + signal);
      conn.end();
    }).on('data', (data) => {
      console.log('STDOUT: ' + data);
    }).stderr.on('data', (data) => {
      console.log('STDERR: ' + data);
    });
  });
}).connect({
  host: '5.182.26.100',
  port: 22,
  username: 'root',
  password: '5PMzEWGQl6O&od!r'
});
