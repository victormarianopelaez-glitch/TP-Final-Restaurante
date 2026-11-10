<script setup>
import { ref, onMounted } from 'vue'
import api, { API_BASE_URL } from '../api/axios'

const tabActiva = ref('productos')
const mensaje = ref('')
const error = ref('')


const productos = ref([])
const prodPage = ref(1)
const prodTotalPages = ref(1)
const prodTotalItems = ref(0)
const prodFiltroBusqueda = ref('')
const prodFiltroCategoria = ref('')
const archivoImagenProducto = ref(null)

const formProducto = ref({
  idProducto: 0,
  idCategoria: '',
  nombre: '',
  descripcion: '',
  precioVigente: '',
  estado: 'Disponible'
})


const categoriasTodas = ref([])
const categoriasPaginadas = ref([])
const catPage = ref(1)
const catTotalPages = ref(1)
const formCategoria = ref({ idCategoria: 0, nombre: '', descripcion: '' })


const mesas = ref([])
const formMesa = ref({ idMesa: 0, numero: '', capacidad: 4 })


const usuarios = ref([])
const userPage = ref(1)
const userTotalPages = ref(1)
const formUsuario = ref({
  idUsuario: 0,
  nombre: '',
  apellido: '',
  telefono: '',
  email: '',
  password: '',
  rol: 'Mozo'
})

const limpiarAlertas = () => {
  mensaje.value = ''
  error.value = ''
}


const cargarCategorias = async (page = 1) => {
  try {
    const allRes = await api.get('/categorias?all=true')
    categoriasTodas.value = allRes.data
    const pagRes = await api.get(`/categorias?page=${page}&pageSize=6`)
    categoriasPaginadas.value = pagRes.data.items
    catPage.value = pagRes.data.page
    catTotalPages.value = pagRes.data.totalPages || 1
  } catch {}
}


const cargarProductos = async (page = 1) => {
  try {
    const params = new URLSearchParams({ page, pageSize: 6 })
    if (prodFiltroBusqueda.value.trim()) params.append('search', prodFiltroBusqueda.value.trim())
    if (prodFiltroCategoria.value) params.append('idCategoria', prodFiltroCategoria.value)
    const { data } = await api.get(`/productos?${params.toString()}`)
    productos.value = data.items
    prodPage.value = data.page
    prodTotalPages.value = data.totalPages || 1
    prodTotalItems.value = data.totalItems
  } catch {}
}

const onFileChangeProducto = (e) => {
  archivoImagenProducto.value = e.target.files?.[0] || null
}

const guardarProducto = async () => {
  limpiarAlertas()
  try {
    const fd = new FormData()
    fd.append('idCategoria', formProducto.value.idCategoria)
    fd.append('nombre', formProducto.value.nombre)
    fd.append('descripcion', formProducto.value.descripcion || '')
    fd.append('precioVigente', formProducto.value.precioVigente)
    fd.append('estado', formProducto.value.estado)
    if (archivoImagenProducto.value) {
      fd.append('imagen', archivoImagenProducto.value)
    }

    if (formProducto.value.idProducto > 0) {
      await api.put(`/productos/${formProducto.value.idProducto}`, fd, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
      mensaje.value = 'Producto actualizado correctamente.'
    } else {
      await api.post('/productos', fd, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
      mensaje.value = 'Producto dado de alta en el menú.'
    }
    resetFormProducto()
    await cargarProductos(prodPage.value)
  } catch (err) {
    error.value = err.response?.data?.mensaje || 'Error al guardar el producto.'
  }
}

const editarProductoForm = (p) => {
  formProducto.value = {
    idProducto: p.idProducto,
    idCategoria: p.idCategoria,
    nombre: p.nombre,
    descripcion: p.descripcion || '',
    precioVigente: p.precioVigente,
    estado: p.estado
  }
  archivoImagenProducto.value = null
}

const resetFormProducto = () => {
  formProducto.value = {
    idProducto: 0,
    idCategoria: categoriasTodas.value[0]?.idCategoria || '',
    nombre: '',
    descripcion: '',
    precioVigente: '',
    estado: 'Disponible'
  }
  archivoImagenProducto.value = null
}

const alternarStockProducto = async (p) => {
  limpiarAlertas()
  const nuevoEstado = p.estado === 'Disponible' ? 'Sin stock' : 'Disponible'
  try {
    await api.patch(`/productos/${p.idProducto}/estado`, { estado: nuevoEstado })
    mensaje.value = `Estado de "${p.nombre}" cambiado a ${nuevoEstado}.`
    await cargarProductos(prodPage.value)
  } catch (err) {
    error.value = 'Error al cambiar disponibilidad del producto.'
  }
}

const eliminarProducto = async (id) => {
  if (!confirm('¿Dar de baja este producto del catálogo?')) return
  limpiarAlertas()
  try {
    await api.delete(`/productos/${id}`)
    mensaje.value = 'Producto dado de baja.'
    await cargarProductos(prodPage.value)
  } catch (err) {
    error.value = 'No se pudo eliminar el producto.'
  }
}


const guardarCategoria = async () => {
  limpiarAlertas()
  try {
    if (formCategoria.value.idCategoria > 0) {
      await api.put(`/categorias/${formCategoria.value.idCategoria}`, formCategoria.value)
      mensaje.value = 'Categoría modificada.'
    } else {
      await api.post('/categorias', formCategoria.value)
      mensaje.value = 'Categoría creada.'
    }
    formCategoria.value = { idCategoria: 0, nombre: '', descripcion: '' }
    await cargarCategorias(catPage.value)
  } catch (err) {
    error.value = err.response?.data?.mensaje || 'Error al guardar categoría.'
  }
}

const eliminarCategoria = async (id) => {
  if (!confirm('¿Dar de baja esta categoría?')) return
  limpiarAlertas()
  try {
    await api.delete(`/categorias/${id}`)
    mensaje.value = 'Categoría eliminada lógicamente.'
    await cargarCategorias(catPage.value)
  } catch (err) {
    error.value = 'No se pudo eliminar la categoría.'
  }
}


const cargarMesas = async () => {
  try {
    const { data } = await api.get('/mesas')
    mesas.value = data
  } catch {}
}

const guardarMesa = async () => {
  limpiarAlertas()
  try {
    if (formMesa.value.idMesa > 0) {
      await api.put(`/mesas/${formMesa.value.idMesa}`, formMesa.value)
      mensaje.value = 'Mesa actualizada.'
    } else {
      await api.post('/mesas', formMesa.value)
      mensaje.value = 'Mesa creada.'
    }
    formMesa.value = { idMesa: 0, numero: '', capacidad: 4 }
    await cargarMesas()
  } catch (err) {
    error.value = err.response?.data?.mensaje || 'Error al guardar mesa.'
  }
}

const eliminarMesa = async (id) => {
  if (!confirm('¿Dar de baja esta mesa?')) return
  limpiarAlertas()
  try {
    await api.delete(`/mesas/${id}`)
    mensaje.value = 'Mesa dada de baja.'
    await cargarMesas()
  } catch (err) {
    error.value = err.response?.data?.mensaje || 'No se pudo eliminar la mesa.'
  }
}

// CRUD Usuarios
const cargarUsuarios = async (page = 1) => {
  try {
    const { data } = await api.get(`/usuarios?page=${page}&pageSize=6`)
    usuarios.value = data.items
    userPage.value = data.page
    userTotalPages.value = data.totalPages || 1
  } catch {}
}

const guardarUsuario = async () => {
  limpiarAlertas()
  try {
    if (formUsuario.value.idUsuario > 0) {
      await api.put(`/usuarios/${formUsuario.value.idUsuario}`, formUsuario.value)
      mensaje.value = 'Usuario actualizado.'
    } else {
      await api.post('/usuarios', formUsuario.value)
      mensaje.value = 'Usuario creado exitosamente.'
    }
    formUsuario.value = { idUsuario: 0, nombre: '', apellido: '', telefono: '', email: '', password: '', rol: 'Mozo' }
    await cargarUsuarios(userPage.value)
  } catch (err) {
    error.value = err.response?.data?.mensaje || 'Error al guardar usuario.'
  }
}

const eliminarUsuario = async (id) => {
  if (!confirm('¿Dar de baja este usuario?')) return
  limpiarAlertas()
  try {
    await api.delete(`/usuarios/${id}`)
    mensaje.value = 'Usuario dado de baja.'
    await cargarUsuarios(userPage.value)
  } catch (err) {
    error.value = 'Error al dar de baja al usuario.'
  }
}

onMounted(async () => {
  await cargarCategorias(1)
  resetFormProducto()
  await cargarProductos(1)
  await cargarMesas()
  await cargarUsuarios(1)
})
</script>

<template>
  <div class="container">
    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; flex-wrap: wrap; gap: 10px;">
      <h1>Panel de Administración (ABM Reactivo SPA)</h1>
      <div style="display: flex; gap: 8px;">
        <button class="btn" :class="tabActiva === 'productos' ? 'btn-primary' : 'btn-secondary'" @click="tabActiva = 'productos'">
          🍔 Productos
        </button>
        <button class="btn" :class="tabActiva === 'categorias' ? 'btn-primary' : 'btn-secondary'" @click="tabActiva = 'categorias'">
          📂 Categorías
        </button>
        <button class="btn" :class="tabActiva === 'mesas' ? 'btn-primary' : 'btn-secondary'" @click="tabActiva = 'mesas'">
          🪑 Mesas
        </button>
        <button class="btn" :class="tabActiva === 'usuarios' ? 'btn-primary' : 'btn-secondary'" @click="tabActiva = 'usuarios'">
          👥 Usuarios y Mozos
        </button>
      </div>
    </div>

    <div v-if="mensaje" class="alert alert-ok">{{ mensaje }}</div>
    <div v-if="error" class="alert alert-error">{{ error }}</div>


    <div v-if="tabActiva === 'productos'" class="grid-2">
      <div class="card">
        <h3>{{ formProducto.idProducto > 0 ? 'Editar Producto #' + formProducto.idProducto : 'Alta de Nuevo Producto' }}</h3>
        <form @submit.prevent="guardarProducto" style="margin-top: 12px;">
          <div style="margin-bottom: 10px;">
            <label>Categoría</label>
            <select v-model="formProducto.idCategoria" required>
              <option v-for="c in categoriasTodas" :key="c.idCategoria" :value="c.idCategoria">{{ c.nombre }}</option>
            </select>
          </div>
          <div style="margin-bottom: 10px;">
            <label>Nombre del Plato / Producto</label>
            <input type="text" v-model="formProducto.nombre" required placeholder="Ej. Milanesa Napolitana" />
          </div>
          <div style="margin-bottom: 10px;">
            <label>Descripción</label>
            <textarea v-model="formProducto.descripcion" rows="2" placeholder="Ingredientes o detalle..."></textarea>
          </div>
          <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin-bottom: 10px;">
            <div>
              <label>Precio Vigente ($)</label>
              <input type="number" step="0.01" min="0" v-model="formProducto.precioVigente" required />
            </div>
            <div>
              <label>Disponibilidad</label>
              <select v-model="formProducto.estado">
                <option value="Disponible">Disponible</option>
                <option value="Sin stock">Sin stock (Suspendido)</option>
              </select>
            </div>
          </div>
          <div style="margin-bottom: 14px;">
            <label>Imagen Representativa (Almacenada en Servidor)</label>
            <input type="file" accept="image/*" @change="onFileChangeProducto" />
          </div>
          <div style="display: flex; gap: 8px;">
            <button type="submit" class="btn btn-primary">
              {{ formProducto.idProducto > 0 ? 'Guardar Cambios' : 'Crear Producto' }}
            </button>
            <button v-if="formProducto.idProducto > 0" type="button" class="btn btn-secondary" @click="resetFormProducto">
              Cancelar
            </button>
          </div>
        </form>
      </div>

      <div class="card">
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px; gap: 8px; flex-wrap: wrap;">
          <h3>Catálogo de Productos ({{ prodTotalItems }})</h3>
          <div style="display: flex; gap: 6px;">
            <input type="text" v-model="prodFiltroBusqueda" @input="cargarProductos(1)" placeholder="Buscar..." style="width: 150px;" />
            <select v-model="prodFiltroCategoria" @change="cargarProductos(1)" style="width: 150px;">
              <option value="">Todas</option>
              <option v-for="c in categoriasTodas" :key="c.idCategoria" :value="c.idCategoria">{{ c.nombre }}</option>
            </select>
          </div>
        </div>

        <table class="table">
          <thead>
            <tr>
              <th>Img</th>
              <th>Producto</th>
              <th>Precio</th>
              <th>Estado</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="p in productos" :key="p.idProducto">
              <td>
                <img
                  v-if="p.imagenRuta"
                  :src="`${API_BASE_URL}${p.imagenRuta}`"
                  style="width: 40px; height: 40px; object-fit: cover; border-radius: 6px;"
                />
                <span v-else style="font-size: 0.75rem; color: #94a3b8;">Sin foto</span>
              </td>
              <td>
                <strong>{{ p.nombre }}</strong>
                <div style="font-size: 0.76rem; color: #64748b;">{{ p.categoria?.nombre }}</div>
              </td>
              <td><strong>${{ p.precioVigente.toFixed(2) }}</strong></td>
              <td>
                <span class="badge" :class="p.estado === 'Disponible' ? 'badge-success' : 'badge-danger'">
                  {{ p.estado }}
                </span>
              </td>
              <td style="display: flex; gap: 4px; flex-wrap: wrap;">
                <button class="btn btn-sm btn-secondary" @click="editarProductoForm(p)">Editar</button>
                <button class="btn btn-sm btn-warning" @click="alternarStockProducto(p)">
                  {{ p.estado === 'Disponible' ? 'Suspender' : 'Activar' }}
                </button>
                <button class="btn btn-sm btn-danger" @click="eliminarProducto(p.idProducto)">Baja</button>
              </td>
            </tr>
          </tbody>
        </table>

        <div class="pagination">
          <span style="font-size: 0.82rem; color: #64748b;">Página {{ prodPage }} de {{ prodTotalPages }}</span>
          <div style="display: flex; gap: 6px;">
            <button class="btn btn-sm btn-secondary" :disabled="prodPage <= 1" @click="cargarProductos(prodPage - 1)">← Ant.</button>
            <button class="btn btn-sm btn-secondary" :disabled="prodPage >= prodTotalPages" @click="cargarProductos(prodPage + 1)">Sig. →</button>
          </div>
        </div>
      </div>
    </div>


    <div v-if="tabActiva === 'categorias'" class="grid-2">
      <div class="card">
        <h3>{{ formCategoria.idCategoria > 0 ? 'Editar Categoría' : 'Nueva Categoría' }}</h3>
        <form @submit.prevent="guardarCategoria" style="margin-top: 12px;">
          <div style="margin-bottom: 10px;">
            <label>Nombre</label>
            <input type="text" v-model="formCategoria.nombre" required placeholder="Ej. Postres" />
          </div>
          <div style="margin-bottom: 12px;">
            <label>Descripción</label>
            <input type="text" v-model="formCategoria.descripcion" placeholder="Descripción opcional..." />
          </div>
          <div style="display: flex; gap: 8px;">
            <button type="submit" class="btn btn-primary">Guardar Categoría</button>
            <button v-if="formCategoria.idCategoria > 0" type="button" class="btn btn-secondary" @click="formCategoria = { idCategoria: 0, nombre: '', descripcion: '' }">
              Cancelar
            </button>
          </div>
        </form>
      </div>

      <div class="card">
        <h3>Listado de Categorías</h3>
        <table class="table" style="margin-top: 10px;">
          <thead>
            <tr>
              <th>ID</th>
              <th>Nombre</th>
              <th>Descripción</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="c in categoriasPaginadas" :key="c.idCategoria">
              <td>#{{ c.idCategoria }}</td>
              <td><strong>{{ c.nombre }}</strong></td>
              <td>{{ c.descripcion || '-' }}</td>
              <td style="display: flex; gap: 6px;">
                <button class="btn btn-sm btn-secondary" @click="formCategoria = { ...c }">Editar</button>
                <button class="btn btn-sm btn-danger" @click="eliminarCategoria(c.idCategoria)">Eliminar</button>
              </td>
            </tr>
          </tbody>
        </table>
        <div class="pagination">
          <span style="font-size: 0.82rem; color: #64748b;">Página {{ catPage }} de {{ catTotalPages }}</span>
          <div style="display: flex; gap: 6px;">
            <button class="btn btn-sm btn-secondary" :disabled="catPage <= 1" @click="cargarCategorias(catPage - 1)">← Ant.</button>
            <button class="btn btn-sm btn-secondary" :disabled="catPage >= catTotalPages" @click="cargarCategorias(catPage + 1)">Sig. →</button>
          </div>
        </div>
      </div>
    </div>


    <div v-if="tabActiva === 'mesas'" class="grid-2">
      <div class="card">
        <h3>{{ formMesa.idMesa > 0 ? 'Editar Mesa' : 'Alta de Mesa' }}</h3>
        <form @submit.prevent="guardarMesa" style="margin-top: 12px;">
          <div style="margin-bottom: 10px;">
            <label>Número Identificador de Mesa</label>
            <input type="number" min="1" v-model="formMesa.numero" required />
          </div>
          <div style="margin-bottom: 12px;">
            <label>Capacidad Máxima (Comensales)</label>
            <input type="number" min="1" v-model="formMesa.capacidad" required />
          </div>
          <button type="submit" class="btn btn-primary">Guardar Mesa</button>
        </form>
      </div>

      <div class="card">
        <h3>Mesas del Salón</h3>
        <table class="table" style="margin-top: 10px;">
          <thead>
            <tr>
              <th>Mesa #</th>
              <th>Capacidad</th>
              <th>Estado</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="m in mesas" :key="m.idMesa">
              <td><strong>Mesa #{{ m.numero }}</strong></td>
              <td>{{ m.capacidad }} personas</td>
              <td>
                <span class="badge" :class="m.estado === 'Libre' ? 'badge-success' : 'badge-danger'">{{ m.estado }}</span>
              </td>
              <td style="display: flex; gap: 6px;">
                <button class="btn btn-sm btn-secondary" @click="formMesa = { idMesa: m.idMesa, numero: m.numero, capacidad: m.capacidad }">Editar</button>
                <button class="btn btn-sm btn-danger" @click="eliminarMesa(m.idMesa)">Baja</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>


    <div v-if="tabActiva === 'usuarios'" class="grid-2">
      <div class="card">
        <h3>{{ formUsuario.idUsuario > 0 ? 'Editar Usuario' : 'Registrar Usuario / Empleado' }}</h3>
        <form @submit.prevent="guardarUsuario" style="margin-top: 12px;">
          <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin-bottom: 10px;">
            <div>
              <label>Nombre</label>
              <input type="text" v-model="formUsuario.nombre" required />
            </div>
            <div>
              <label>Apellido</label>
              <input type="text" v-model="formUsuario.apellido" required />
            </div>
          </div>
          <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin-bottom: 10px;">
            <div>
              <label>Email</label>
              <input type="email" v-model="formUsuario.email" required />
            </div>
            <div>
              <label>Teléfono</label>
              <input type="text" v-model="formUsuario.telefono" />
            </div>
          </div>
          <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin-bottom: 12px;">
            <div>
              <label>Contraseña {{ formUsuario.idUsuario > 0 ? '(dejar vacío para no cambiar)' : '' }}</label>
              <input type="password" v-model="formUsuario.password" :required="formUsuario.idUsuario === 0" />
            </div>
            <div>
              <label>Rol</label>
              <select v-model="formUsuario.rol">
                <option value="Mozo">Mozo (Empleado)</option>
                <option value="Administrador">Administrador</option>
              </select>
            </div>
          </div>
          <button type="submit" class="btn btn-primary">Guardar Usuario</button>
        </form>
      </div>

      <div class="card">
        <h3>Personal Registrado</h3>
        <table class="table" style="margin-top: 10px;">
          <thead>
            <tr>
              <th>Nombre</th>
              <th>Email</th>
              <th>Rol</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="u in usuarios" :key="u.idUsuario">
              <td><strong>{{ u.nombre }} {{ u.apellido }}</strong></td>
              <td>{{ u.email }}</td>
              <td><span class="badge" :class="u.rol === 'Administrador' ? 'badge-warning' : 'badge-info'">{{ u.rol }}</span></td>
              <td style="display: flex; gap: 6px;">
                <button class="btn btn-sm btn-secondary" @click="formUsuario = { ...u, password: '' }">Editar</button>
                <button class="btn btn-sm btn-danger" @click="eliminarUsuario(u.idUsuario)">Baja</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
