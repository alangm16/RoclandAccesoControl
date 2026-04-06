/**
 * proveedor-form.js
 * Rocland — Sistema de Control de Acceso
 * Sprint 2 — Envío del formulario de proveedor/cliente via fetch API
 */

(function () {
    'use strict';

    const form       = document.getElementById('frmProveedor');
    const btnEnviar  = document.getElementById('btnEnviar');
    const btnText    = document.getElementById('btnText');
    const btnSpinner = document.getElementById('btnSpinner');
    const btnIcon    = document.getElementById('btnIcon');
    const alertError = document.getElementById('alertError');
    const alertMsg   = document.getElementById('alertErrorMsg');

    if (!form) return;

    function setLoading(loading) {
        btnEnviar.disabled = loading;
        btnText.textContent = loading ? 'Enviando...' : 'Enviar Solicitud';
        btnSpinner.classList.toggle('d-none', !loading);
        btnIcon.classList.toggle('d-none', loading);
    }

    function showError(msg) {
        alertMsg.textContent = msg;
        alertError.classList.add('show');
        alertError.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }

    function hideError() {
        alertError.classList.remove('show');
    }

    function buildPayload() {
        // Convertir placas a mayúsculas
        const placas = form.UnidadPlacas?.value.trim().toUpperCase() || null;

        return {
            nombre               : form.Nombre.value.trim(),
            tipoIdentificacionId : parseInt(form.TipoIdentificacionId.value, 10),
            numeroIdentificacion : form.NumeroIdentificacion.value.trim(),
            empresa              : form.Empresa.value.trim(),
            telefono             : form.Telefono?.value.trim()        || null,
            email                : form.Email?.value.trim()           || null,
            motivoId             : parseInt(form.MotivoId.value, 10),
            unidadPlacas         : placas,
            facturaRemision      : form.FacturaRemision?.value.trim() || null,
            consentimientoFirmado: form.ConsentimientoFirmado.checked,
            observaciones        : form.Observaciones?.value.trim()   || null,
        };
    }

    function getAntiForgeryToken() {
        const el = form.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    form.addEventListener('submit', async function (e) {
        e.preventDefault();
        hideError();

        if (!form.checkValidity()) {
            form.classList.add('was-validated');
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

            const res = await fetch('/api/proveedores', {
                method : 'POST',
                headers: {
                    'Content-Type'             : 'application/json',
                    'RequestVerificationToken' : getAntiForgeryToken(),
                },
                body: JSON.stringify(payload),
            });

            if (res.ok) {
                const data = await res.json();

                sessionStorage.setItem('rocland_confirm', JSON.stringify({
                    nombre : data.nombre,
                    tipo   : 'Proveedor / Cliente',
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
            console.error('[ProveedorForm] Error:', err);
            showError('Error de conexión. Verifica tu red e intenta de nuevo.');
        } finally {
            setLoading(false);
        }
    });

})();
