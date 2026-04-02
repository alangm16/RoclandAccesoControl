/**
 * autocompletado.js
 * Rocland — Sistema de Control de Acceso
 * Sprint 2 — Autocompletado por número de identificación
 *
 * Escucha cambios en #numeroIdentificacion, llama a
 * GET /api/personas/buscar?numId=xxx y prellenan campos.
 */

(function () {
    'use strict';

    const DEBOUNCE_MS = 500;     // Espera 500 ms tras dejar de escribir
    const MIN_CHARS   = 3;       // Mínimo de caracteres para buscar

    let _debounceTimer = null;
    let _lastQuery     = '';

    /** Referencias DOM */
    const numIdInput     = document.getElementById('numeroIdentificacion');
    const spinner        = document.querySelector('.id-search-spinner');
    const returningBadge = document.getElementById('returningBadge');
    const returningVisits = document.getElementById('returningVisits');

    if (!numIdInput) return;   // Guard: si el campo no existe, salir

    /** Campos a rellenar */
    const FIELDS = {
        nombre   : document.getElementById('nombre'),
        empresa  : document.getElementById('empresa'),
        telefono : document.getElementById('telefono'),
        email    : document.getElementById('email'),
    };

    /** Limpia el autocompletado */
    function clearAutoFill() {
        Object.values(FIELDS).forEach(el => {
            if (el) {
                el.removeAttribute('data-autofilled');
                el.classList.remove('filled');
            }
        });
        if (returningBadge) returningBadge.classList.remove('show');
    }

    /** Aplica los datos al formulario */
    function applyPersona(persona) {
        const map = {
            nombre  : persona.nombre,
            empresa : persona.empresa  || '',
            telefono: persona.telefono || '',
            email   : persona.email    || '',
        };

        Object.entries(map).forEach(([key, val]) => {
            const el = FIELDS[key];
            if (!el || !val) return;
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

    /** Llamada a la API */
    async function buscarPersona(numId) {
        if (spinner) spinner.classList.add('active');
        try {
            const res = await fetch(`/api/personas/buscar?numId=${encodeURIComponent(numId)}`);
            if (res.status === 200) {
                const data = await res.json();
                applyPersona(data);
            } else {
                clearAutoFill();
            }
        } catch (err) {
            console.warn('[Autocompletado] Error de red:', err);
            clearAutoFill();
        } finally {
            if (spinner) spinner.classList.remove('active');
        }
    }

    /** Handler del input con debounce */
    numIdInput.addEventListener('input', function () {
        const val = this.value.trim();

        clearAutoFill();

        if (val === _lastQuery) return;
        _lastQuery = val;

        clearTimeout(_debounceTimer);

        if (val.length < MIN_CHARS) return;

        _debounceTimer = setTimeout(() => buscarPersona(val), DEBOUNCE_MS);
    });

    // Si el campo ya tiene valor al cargar (unlikely pero defensivo)
    if (numIdInput.value.trim().length >= MIN_CHARS) {
        buscarPersona(numIdInput.value.trim());
    }

})();
