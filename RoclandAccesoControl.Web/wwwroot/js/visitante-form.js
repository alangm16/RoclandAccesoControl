/**
 * visitante-form.js
 * Rocland — Sistema de Control de Acceso
 * Sprint 2 — Envío del formulario de visitante via fetch API
 */

(function () {
    'use strict';

    const form       = document.getElementById('frmVisitante');
    const btnEnviar  = document.getElementById('btnEnviar');
    const btnText    = document.getElementById('btnText');
    const btnSpinner = document.getElementById('btnSpinner');
    const btnIcon    = document.getElementById('btnIcon');
    const alertError = document.getElementById('alertError');
    const alertMsg   = document.getElementById('alertErrorMsg');

    if (!form) return;

    /** Muestra / oculta estado de carga en el botón */
    function setLoading(loading) {
        btnEnviar.disabled = loading;
        btnText.textContent = loading ? 'Enviando...' : 'Enviar Solicitud';
        btnSpinner.classList.toggle('d-none', !loading);
        btnIcon.classList.toggle('d-none', loading);
    }

    /** Muestra mensaje de error global */
    function showError(msg) {
        alertMsg.textContent = msg;
        alertError.classList.add('show');
        alertError.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }

    function hideError() {
        alertError.classList.remove('show');
    }

    /** Marca todos los campos inválidos de Bootstrap */
    function triggerBsValidation() {
        form.classList.add('was-validated');
    }

    /** Construye el payload desde el formulario */
    function buildPayload() {
        return {
            nombre               : form.Nombre.value.trim(),
            tipoIdentificacionId : parseInt(form.TipoIdentificacionId.value, 10),
            numeroIdentificacion : form.NumeroIdentificacion.value.trim(),
            empresa              : form.Empresa?.value.trim()    || null,
            telefono             : form.Telefono?.value.trim()   || null,
            email                : form.Email?.value.trim()      || null,
            areaId               : parseInt(form.AreaId.value, 10),
            motivoId             : parseInt(form.MotivoId.value, 10),
            consentimientoFirmado: form.ConsentimientoFirmado.checked,
            observaciones        : form.Observaciones?.value.trim() || null,
        };
    }

    /** Obtiene el token anti-forgery del campo oculto Razor */
    function getAntiForgeryToken() {
        const el = form.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    form.addEventListener('submit', async function (e) {
        e.preventDefault();
        hideError();

        // Validación nativa HTML5
        if (!form.checkValidity()) {
            triggerBsValidation();
            // Scroll al primer campo inválido
            const firstInvalid = form.querySelector(':invalid');
            if (firstInvalid) {
                firstInvalid.scrollIntoView({ behavior: 'smooth', block: 'center' });
                firstInvalid.focus();
            }
            return;
        }

        setLoading(true);

        try {
            const payload = buildPayload();

            const res = await fetch('/api/visitantes', {
                method : 'POST',
                headers: {
                    'Content-Type'              : 'application/json',
                    'RequestVerificationToken'  : getAntiForgeryToken(),
                },
                body: JSON.stringify(payload),
            });

            if (res.ok) {
                const data = await res.json();

                // Guardar datos de confirmación en sessionStorage
                sessionStorage.setItem('rocland_confirm', JSON.stringify({
                    nombre : data.nombre,
                    tipo   : 'Visitante',
                    id     : data.id,
                    hora   : new Date(data.fechaEntrada).toLocaleTimeString('es-MX', {
                                 hour: '2-digit', minute: '2-digit'
                             }),
                }));

                window.location.href = '/Acceso/Confirmacion';

            } else if (res.status === 429) {
                showError('Has enviado demasiadas solicitudes. Espera unos minutos e intenta de nuevo.');
            } else {
                const errText = await res.text();
                showError(errText || 'No se pudo procesar la solicitud. Intenta de nuevo.');
            }

        } catch (err) {
            console.error('[VisitanteForm] Error:', err);
            showError('Error de conexión. Verifica tu red e intenta de nuevo.');
        } finally {
            setLoading(false);
        }
    });

})();
