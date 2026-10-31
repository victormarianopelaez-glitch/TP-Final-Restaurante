<script setup>
    import { ref } from 'vue'

    const email = ref('')
    const password = ref('')

    const iniciarSesion = () => {
        alert(`Intentando loguear con: ${email.value}`)
    }

</script>

<template>
    <div style="max-width: 350px; margin: 50px auto; padding: 20px; border: 1px solid #ccc; border-radius: 8px;">
        <h2>Iniciar Sesion</h2>

        <div>
            <label>Email</label>
            <input type="email" v-model="email" placeholder="usuario@resto.com" style="width: 100%; margin-bottom: 10px;">
        </div>

        <div>
            <label>Contraseña</label>
            <input type="password" v-model="password" placeholder="Contraseña" style="width: 100%; margin-bottom: 15px;">
        </div>

        <button @click="iniciarSesion" style="width: 100% ; padding: 8px; cursor: pointer;">
            Ingresar
        </button>

    </div>
</template>
