/**
 * autocompletado.js
 * Rocland — Sistema de Control de Acceso
 * Sprint 2 — Autocompletado por número de identificación (búsqueda exacta)
 *
 * Escucha cambios en #numeroIdentificacion, llama a
 * GET /api/personas/buscar?numId=xxx y prellenan campos.
 * SOLO cuando el número coincide exactamente con un registro existente.
 */

(function () {
    'use strict';

    const DEBOUNCE_MS = 500;     // Espera 500 ms tras dejar de escribir
    const MIN_CHARS = 3;       // Mínimo de caracteres para buscar (sugerencia: 3)

    let _debounceTimer = null;
    let _lastQuery = '';
    let _abortController = null;   // Para cancelar peticiones anteriores
    let _lastNumId = '';      // Último número de identidad que se está buscando

    /** Referencias DOM */
    const numIdInput = document.getElementById('numeroIdentificacion');
    const spinner = document.querySelector('.id-search-spinner');
    const returningBadge = document.getElementById('returningBadge');
    const returningVisits = document.getElementById('returningVisits');

    if (!numIdInput) return;   // Guard: si el campo no existe, salir

    /** Campos a rellenar */
    const FIELDS = {
        nombre: document.getElementById('nombre'),
        empresa: document.getElementById('empresa'),
        telefono: document.getElementById('telefono'),
        email: document.getElementById('email'),
    };

    /** 
     * Limpia el autocompletado (valor + atributos + badge)
     * IMPORTANTE: vacía el value de cada campo.
     */
    function clearAutoFill() {
        Object.values(FIELDS).forEach(el => {
            if (el) {
                el.value = '';                         // ← vacía el campo
                el.removeAttribute('data-autofilled');
                el.classList.remove('filled');
            }
        });
        if (returningBadge) returningBadge.classList.remove('show');
    }

    /** Aplica los datos al formulario (solo si el ID aún coincide) */
    function applyPersona(persona, currentNumId) {
        // Si el número de identificación actual ya no es el mismo que generó esta respuesta, ignorar
        if (currentNumId !== _lastNumId) {
            console.debug('[autocompletado] Respuesta obsoleta, ignorada');
            return;
        }

        const map = {
            nombre: persona.nombre,
            empresa: persona.empresa || '',
            telefono: persona.telefono || '',
            email: persona.email || '',
        };

        Object.entries(map).forEach(([key, val]) => {
            const el = FIELDS[key];
            if (!el) return;
            el.value = val;
            el.setAttribute('data-autofilled', '1');
            el.classList.add('filled');
        });

        // Mostrar badge de visitante recurrente
        if (returningBadge && persona.totalVisitas > 0) {
            if (returningVisits) returningVisits.textContent = persona.totalVisitas;
            returningBadge.classList.add('show');
        }
    }

    /** Llamada a la API con cancelación de petición anterior */
    async function buscarPersona(numId) {
        // Cancelar petición anterior si existe
        if (_abortController) {
            _abortController.abort();
        }
        _abortController = new AbortController();
        const signal = _abortController.signal;

        // Guardar el número que estamos buscando para validar después
        _lastNumId = numId;

        if (spinner) spinner.classList.add('active');
        try {
            const res = await fetch(`/api/personas/buscar?numId=${encodeURIComponent(numId)}`, { signal });
            if (res.status === 200) {
                const data = await res.json();
                applyPersona(data, numId);
            } else if (res.status === 404) {
                // No se encontró persona con ese número exacto → limpiar campos
                // Pero solo si el número actual sigue siendo el mismo (evita limpiar mientras escribe)
                if (numId === _lastNumId) {
                    clearAutoFill();
                }
            } else {
                // Error inesperado (500, etc.) → limpiar por seguridad
                if (numId === _lastNumId) clearAutoFill();
            }
        } catch (err) {
            // Si es abort, no es error; ignorar silenciosamente
            if (err.name === 'AbortError') {
                console.debug('[autocompletado] Petición cancelada');
            } else {
                console.warn('[Autocompletado] Error de red:', err);
                if (numId === _lastNumId) clearAutoFill();
            }
        } finally {
            if (spinner) spinner.classList.remove('active');
            // No limpiar _abortController aquí, se reemplazará en la próxima llamada
        }
    }

    /** Handler del input con debounce */
    numIdInput.addEventListener('input', function () {
        const val = this.value.trim();

        // Limpiar campos inmediatamente mientras escribe/borra
        clearAutoFill();

        if (val === _lastQuery) return;
        _lastQuery = val;

        clearTimeout(_debounceTimer);

        if (val.length < MIN_CHARS) return;

        _debounceTimer = setTimeout(() => buscarPersona(val), DEBOUNCE_MS);
    });

    // Si el campo ya tiene valor al cargar (defensivo)
    if (numIdInput.value.trim().length >= MIN_CHARS) {
        buscarPersona(numIdInput.value.trim());
    }
})();