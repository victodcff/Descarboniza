/* Cadastro: olhinho, requisitos da senha e aviso de senhas diferentes.
   Nunca apaga os campos: só avisa. */
(function () {
    'use strict';

    var form = document.getElementById('registerForm');
    var senha = document.getElementById('Input_Password');
    var conf = document.getElementById('Input_ConfirmPassword');
    var lista = document.getElementById('senhaReq');
    var aviso = document.getElementById('senhaAviso');
    if (!form || !senha || !conf) return;

    var regras = {
        len: function (v) { return v.length >= 6; },
        upper: function (v) { return /[A-Z]/.test(v); },
        lower: function (v) { return /[a-z]/.test(v); },
        num: function (v) { return /[0-9]/.test(v); },
        sym: function (v) { return /[^A-Za-z0-9]/.test(v); }
    };

    // Olhinho
    document.querySelectorAll('[data-senha-toggle]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var campo = btn.parentElement.querySelector('input');
            var mostrar = campo.type === 'password';
            campo.type = mostrar ? 'text' : 'password';
            btn.setAttribute('aria-pressed', mostrar ? 'true' : 'false');
            btn.setAttribute('aria-label', mostrar ? 'Ocultar senha' : 'Mostrar senha');
            campo.focus();
        });
    });

    function requisitosOk() {
        var ok = true;
        if (lista) {
            lista.querySelectorAll('li').forEach(function (li) {
                var passou = regras[li.getAttribute('data-req')](senha.value);
                li.classList.toggle('ok', passou);
                if (!passou) ok = false;
            });
        } else {
            for (var k in regras) if (!regras[k](senha.value)) ok = false;
        }
        return ok;
    }

    function diferentes() {
        return conf.value.length > 0 && senha.value !== conf.value;
    }

    function mostrarAviso(mostrar) {
        if (aviso) aviso.textContent = mostrar ? 'As senhas não coincidem.' : '';
        conf.classList.toggle('input-validation-error', mostrar);
    }

    senha.addEventListener('input', function () {
        requisitosOk();
        if (lista && lista.classList.contains('erro') && requisitosOk()) lista.classList.remove('erro');
        if (conf.value) mostrarAviso(diferentes());
    });

    conf.addEventListener('input', function () { mostrarAviso(diferentes()); });

    requisitosOk();

    // Bloqueia o envio sem limpar nada
    form.addEventListener('submit', function (e) {
        var falhou = false;

        if (!requisitosOk()) {
            if (lista) lista.classList.add('erro');
            senha.focus();
            falhou = true;
        }

        if (senha.value !== conf.value) {
            mostrarAviso(true);
            if (!falhou) conf.focus();
            falhou = true;
        }

        if (falhou) {
            e.preventDefault(); // forms.js ignora submits cancelados (sem "Criando conta…")
        }
    });
})();
