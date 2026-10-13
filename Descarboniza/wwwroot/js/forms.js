(function () {
    "use strict";

    // Botões que entram em estado de carregamento ao enviar
    var SELETOR =
        'button[type="submit"].form-submit, button[type="submit"].delete-confirm';

    function textoDeCarregamento(botao) {

        if (botao.dataset.loadingText) {
            return botao.dataset.loadingText;
        }

        return botao.classList.contains("delete-confirm")
            ? "Excluindo…"
            : "Salvando…";
    }

    function criarLoader() {

        var loader = document.createElement("span");

        loader.className = "dsc-loader";
        loader.setAttribute("aria-hidden", "true");

        for (var i = 0; i < 3; i++) {

            var ponto = document.createElement("span");
            ponto.className = "dsc-loader__dot";
            loader.appendChild(ponto);
        }

        return loader;
    }

    function ativar(botao) {

        var texto = document.createElement("span");
        texto.textContent = textoDeCarregamento(botao);

        botao.dataset.conteudoOriginal = botao.innerHTML;

        // Mantém a largura para o botão não "pular"
        botao.style.minWidth = botao.offsetWidth + "px";

        botao.classList.add("is-loading");
        botao.setAttribute("aria-busy", "true");

        botao.innerHTML = "";
        botao.appendChild(criarLoader());
        botao.appendChild(texto);
    }

    function restaurar(botao) {

        if (botao.dataset.conteudoOriginal !== undefined) {

            botao.innerHTML = botao.dataset.conteudoOriginal;
            delete botao.dataset.conteudoOriginal;
        }

        botao.classList.remove("is-loading");
        botao.removeAttribute("aria-busy");
        botao.style.minWidth = "";
    }

    document.addEventListener("submit", function (evento) {

        var formulario = evento.target;

        if (!(formulario instanceof HTMLFormElement)) {
            return;
        }

        // Já está enviando: bloqueia o segundo envio
        if (formulario.dataset.enviando === "true") {
            evento.preventDefault();
            return;
        }

        // A validação do formulário barrou o envio
        if (evento.defaultPrevented) {
            return;
        }

        var botao = evento.submitter || formulario.querySelector(SELETOR);

        if (!botao || !botao.matches(SELETOR)) {
            return;
        }

        formulario.dataset.enviando = "true";
        ativar(botao);
    });

    // Voltar pelo navegador não pode deixar o botão travado
    window.addEventListener("pageshow", function (evento) {

        if (!evento.persisted) {
            return;
        }

        document
            .querySelectorAll(".is-loading")
            .forEach(restaurar);

        document
            .querySelectorAll('form[data-enviando="true"]')
            .forEach(function (formulario) {
                delete formulario.dataset.enviando;
            });
    });


    // Relógio dos campos de data automática (somente leitura)
    function dois(numero) {
        return String(numero).padStart(2, "0");
    }

    function formatarAgora() {

        var agora = new Date();

        return dois(agora.getDate()) + "/" +
            dois(agora.getMonth() + 1) + "/" +
            agora.getFullYear() + " " +
            dois(agora.getHours()) + ":" +
            dois(agora.getMinutes());
    }

    function atualizarRelogios() {

        document
            .querySelectorAll("input[data-relogio]")
            .forEach(function (campo) {
                campo.value = formatarAgora();
            });
    }

    atualizarRelogios();
    setInterval(atualizarRelogios, 30000);

})();