(function () {
    "use strict";

    function fechar(toast) {

        if (toast.dataset.fechando === "true") {
            return;
        }

        toast.dataset.fechando = "true";
        toast.classList.add("is-hiding");

        setTimeout(function () {

            var regiao = toast.parentElement;

            toast.remove();

            if (regiao && !regiao.children.length) {
                regiao.remove();
            }

        }, 300);
    }

    function iniciar(toast) {

        var restante = parseInt(toast.dataset.duracao, 10) || 5000;
        var inicio = 0;
        var timer = 0;
        var pausado = false;

        function agendar() {

            inicio = Date.now();

            timer = setTimeout(function () {
                fechar(toast);
            }, restante);
        }

        function pausar() {

            if (pausado) {
                return;
            }

            pausado = true;
            clearTimeout(timer);
            restante -= Date.now() - inicio;
        }

        function retomar() {

            if (!pausado) {
                return;
            }

            pausado = false;
            restante = Math.max(restante, 1500);
            agendar();
        }

        toast.addEventListener("mouseenter", pausar);
        toast.addEventListener("mouseleave", retomar);
        toast.addEventListener("focusin", pausar);
        toast.addEventListener("focusout", retomar);

        var botao = toast.querySelector(".dsc-toast__close");

        if (botao) {
            botao.addEventListener("click", function () {
                fechar(toast);
            });
        }

        agendar();
    }

    document
        .querySelectorAll(".dsc-toast")
        .forEach(iniciar);

})();