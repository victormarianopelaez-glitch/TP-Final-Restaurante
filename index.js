const express = require('express');
const cors = require('cors');
require('dotenv').config();

const app = express();
const PORT = process.env.PORT || 3000;


app.use(cors()); 
app.use(express.json());
app.use(express.urlencoded({ extended: true }));


app.get('/', (req, res) => {
res.json({ mensaje: 'API de RestoGest funcionando correctamente' });
});

const { sequelize } = require('./models');

sequelize.authenticate()
    .then(() => console.log('Conexión a MySQL establecida con éxito.'))
    .catch(err => console.error('Error al conectar con la base de datos:', err));

app.listen(PORT, () => {
console.log(`Servidor de RestoGest corriendo en el puerto ${PORT}`);
});