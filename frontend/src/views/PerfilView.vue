<script setup>
import { ref, onMounted } from 'vue'
import api, { API_BASE_URL } from '../api/axios'

const perfil = ref({
  idUsuario: 0,
  nombre: '',
  apellido: '',
  email: '',
  telefono: '',
  rol: '',
  avatarRuta: ''
})
const nuevaPassword = ref('')
const archivoAvatar = ref(null)
const mensaje = ref('')
const error = ref('')

const cargarPerfil = async () => {
  try {
    const { data } = await api.get('/auth/me')
    perfil.value = data
  } catch {}
}

const onAvatarChange = (e) => {
  archivoAvatar.value = e.target.files?.[0] || null
}

const guardarPerfil = async () => {
  mensaje.value = ''
  error.value = ''
  try {
    const fd = new FormData()
    fd.append('nombre', perfil.value.nombre)
    fd.append('apellido', perfil.value.apellido)
    fd.append('telefono', perfil.value.telefono || '')
    if (nuevaPassword.value.trim()) {
      fd.append('nuevaPassword', nuevaPassword.value.trim())
    }
    if (archivoAvatar.value) {
      fd.append('avatar', archivoAvatar.value)
    }

    const { data } = await api.put('/usuarios/perfil', fd, {
      headers: { 'Content-Type': 'multipart/form-data' }
    })
    perfil.value = data
    nuevaPassword.value = ''
    archivoAvatar.value = null
    localStorage.setItem('usuario', JSON.stringify(data))
    window.dispatchEvent(new Event('storage-user-updated'))
    mensaje.value = 'Perfil y avatar actualizados correctamente.'
  } catch (err) {
    error.value = err.response?.data?.mensaje || 'Error al actualizar el perfil.'
  }
}

onMounted(cargarPerfil)
</script>

<template>
  <div class="container" style="max-width: 600px;">
    <div class="card">
      <h2>👤 Mi Perfil y Avatar Personalizado</h2>
      <p style="font-size: 0.86rem; color: #64748b; margin-bottom: 16px;">
        Actualice sus datos de contacto, contraseña y fotografía de avatar almacenada en el servidor.
      </p>

      <div v-if="mensaje" class="alert alert-ok">{{ mensaje }}</div>
      <div v-if="error" class="alert alert-error">{{ error }}</div>

      <div style="display: flex; align-items: center; gap: 16px; margin-bottom: 18px;">
        <img
          v-if="perfil.avatarRuta"
          :src="`${API_BASE_URL}${perfil.avatarRuta}`"
          alt="Avatar actual"
          style="width: 72px; height: 72px; border-radius: 50%; object-fit: cover; border: 3px solid #2563eb;"
        />
        <div
          v-else
          style="width: 72px; height: 72px; border-radius: 50%; background: #cbd5e1; display: flex; align-items: center; justify-content: center; font-size: 1.6rem; font-weight: bold;"
        >
          {{ perfil.nombre?.[0] || 'U' }}
        </div>
        <div>
          <div style="font-size: 1.1rem; font-weight: bold;">{{ perfil.nombre }} {{ perfil.apellido }}</div>
          <div style="font-size: 0.85rem; color: #64748b;">{{ perfil.email }}</div>
          <span class="badge badge-info">{{ perfil.rol }}</span>
        </div>
      </div>

      <form @submit.prevent="guardarPerfil">
        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-bottom: 12px;">
          <div>
            <label>Nombre</label>
            <input type="text" v-model="perfil.nombre" required />
          </div>
          <div>
            <label>Apellido</label>
            <input type="text" v-model="perfil.apellido" required />
          </div>
        </div>

        <div style="margin-bottom: 12px;">
          <label>Teléfono de Contacto</label>
          <input type="text" v-model="perfil.telefono" placeholder="Ej. 2664001122" />
        </div>

        <div style="margin-bottom: 12px;">
          <label>Nueva Contraseña (opcional, dejar en blanco para mantener la actual)</label>
          <input type="password" v-model="nuevaPassword" placeholder="••••••" />
        </div>

        <div style="margin-bottom: 18px;">
          <label>Subir Nuevo Avatar (Imagen JPG/PNG/WEBP)</label>
          <input type="file" accept="image/*" @change="onAvatarChange" />
        </div>

        <button type="submit" class="btn btn-primary" style="width: 100%;">
          Guardar Cambios de Perfil
        </button>
      </form>
    </div>
  </div>
</template>
