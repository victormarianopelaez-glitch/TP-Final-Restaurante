<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import api from '../api/axios'

const router = useRouter()
const email = ref('')
const password = ref('')
const errorMsg = ref('')
const cargando = ref(false)

const iniciarSesion = async () => {
  errorMsg.value = ''
  if (!email.value || !password.value) {
    errorMsg.value = 'Por favor complete email y contraseña.'
    return
  }
  cargando.value = true
  try {
    const { data } = await api.post('/auth/login', {
      email: email.value,
      password: password.value
    })
    localStorage.setItem('token', data.token)
    localStorage.setItem('usuario', JSON.stringify(data.usuario))
    window.dispatchEvent(new Event('storage-user-updated'))
    if (data.usuario.rol === 'Administrador') {
      router.push('/admin')
    } else {
      router.push('/mozo')
    }
  } catch (err) {
    errorMsg.value = err.response?.data?.mensaje || 'Error al iniciar sesión. Verifique sus credenciales.'
  } finally {
    cargando.value = false
  }
}

const completarDemo = (rol) => {
  if (rol === 'admin') {
    email.value = 'admin@restogest.com'
    password.value = '123456'
  } else {
    email.value = 'mozo@restogest.com'
    password.value = '123456'
  }
}
</script>

<template>
  <div style="min-height: 90vh; display: flex; align-items: center; justify-content: center; padding: 20px;">
    <div class="card" style="width: 100%; max-width: 400px;">
      <h2 style="margin-bottom: 6px; text-align: center;">🍽️ RestoGest</h2>
      <p style="text-align: center; color: #64748b; font-size: 0.88rem; margin-bottom: 20px;">
        Sistema de Gestión de Mesas, Comandas y Facturación
      </p>

      <div v-if="errorMsg" class="alert alert-error">{{ errorMsg }}</div>

      <form @submit.prevent="iniciarSesion">
        <div style="margin-bottom: 12px;">
          <label>Correo Electrónico</label>
          <input type="email" v-model="email" placeholder="admin@restogest.com" required />
        </div>

        <div style="margin-bottom: 18px;">
          <label>Contraseña</label>
          <input type="password" v-model="password" placeholder="••••••" required />
        </div>

        <button type="submit" class="btn btn-primary" style="width: 100%;" :disabled="cargando">
          {{ cargando ? 'Ingresando...' : 'Iniciar Sesión' }}
        </button>
      </form>

      <div style="margin-top: 18px; padding-top: 12px; border-top: 1px solid #e2e8f0; font-size: 0.8rem; color: #64748b;">
        <div style="margin-bottom: 6px;"><strong>Acceso rápido de prueba:</strong></div>
        <div style="display: flex; gap: 8px;">
          <button type="button" class="btn btn-sm btn-secondary" style="flex: 1;" @click="completarDemo('admin')">
            Admin (admin@restogest.com)
          </button>
          <button type="button" class="btn btn-sm btn-secondary" style="flex: 1;" @click="completarDemo('mozo')">
            Mozo (mozo@restogest.com)
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
