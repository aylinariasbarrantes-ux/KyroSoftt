(() => {
    const passwordInput = document.getElementById('password');
    const toggleButton = document.getElementById('togglePassword');
    const eyeOpen = document.getElementById('eyeOpen');
    const eyeClosed = document.getElementById('eyeClosed');

    if (!passwordInput || !toggleButton || !eyeOpen || !eyeClosed) return;

    const setEyeState = (showPassword) => {
        eyeOpen.classList.toggle('scale-100', !showPassword);
        eyeOpen.classList.toggle('rotate-0', !showPassword);
        eyeOpen.classList.toggle('opacity-100', !showPassword);
        eyeOpen.classList.toggle('scale-75', showPassword);
        eyeOpen.classList.toggle('rotate-45', showPassword);
        eyeOpen.classList.toggle('opacity-0', showPassword);

        eyeClosed.classList.toggle('scale-75', !showPassword);
        eyeClosed.classList.toggle('-rotate-45', !showPassword);
        eyeClosed.classList.toggle('opacity-0', !showPassword);
        eyeClosed.classList.toggle('scale-100', showPassword);
        eyeClosed.classList.toggle('rotate-0', showPassword);
        eyeClosed.classList.toggle('opacity-100', showPassword);
    };

    toggleButton.addEventListener('click', () => {
        const showPassword = passwordInput.type === 'password';

        passwordInput.type = showPassword ? 'text' : 'password';

        setEyeState(showPassword);

        toggleButton.setAttribute(
            'aria-label',
            showPassword
                ? 'Ocultar contraseña'
                : 'Mostrar contraseña'
        );
    });
})();


/* ==========================================================
   TOASTS
   ========================================================== */

(() => {

    const dismissToast = (toast) => {

        if (!toast || toast.dataset.closing === 'true') return;

        toast.dataset.closing = 'true';

        toast.classList.remove(
            'translate-x-0',
            'scale-100',
            'opacity-100'
        );

        toast.classList.add(
            'translate-x-6',
            'scale-95',
            'opacity-0'
        );

        window.setTimeout(() => {
            toast.remove();
        }, 300);
    };


    document.querySelectorAll('[data-toast]').forEach((toast) => {

        const closeButton =
            toast.querySelector('[data-toast-close]');

        const progress =
            toast.querySelector('[data-toast-progress]');


        /*
         * Esperamos dos frames.
         *
         * Esto permite que el navegador primero dibuje:
         *
         * scale-x-100
         *
         * y después pueda animar hasta:
         *
         * scale-x-0
         */
        requestAnimationFrame(() => {

            requestAnimationFrame(() => {

                /*
                 * Animación de entrada del toast
                 */
                toast.classList.remove(
                    'translate-x-6',
                    'scale-95',
                    'opacity-0'
                );

                toast.classList.add(
                    'translate-x-0',
                    'scale-100',
                    'opacity-100'
                );


                /*
                 * Animación de la barra inferior
                 */
                if (progress) {

                    progress.classList.remove(
                        'scale-x-100'
                    );

                    progress.classList.add(
                        'scale-x-0'
                    );
                }

            });

        });


        /*
         * Cerrar manualmente con X
         */
        closeButton?.addEventListener('click', () => {
            dismissToast(toast);
        });


        /*
         * Cerrar automáticamente después de 4.5 segundos
         */
        window.setTimeout(() => {
            dismissToast(toast);
        }, 4500);

    });

})();


/* ==========================================================
   DARK / LIGHT MODE
   ========================================================== */

(() => {

    const root = document.documentElement;

    const toggles =
        document.querySelectorAll('[data-theme-toggle]');

    if (!toggles.length) return;


    const applyTheme = (theme, persist = true) => {

        const useDark = theme === 'dark';

        root.classList.toggle(
            'dark',
            useDark
        );

        root.style.colorScheme =
            useDark
                ? 'dark'
                : 'light';


        if (persist) {

            localStorage.setItem(
                'kyro-theme',
                theme
            );
        }


        toggles.forEach((button) => {

            button.setAttribute(
                'aria-label',
                useDark
                    ? 'Activar modo claro'
                    : 'Activar modo oscuro'
            );

            button.setAttribute(
                'title',
                useDark
                    ? 'Cambiar a modo claro'
                    : 'Cambiar a modo oscuro'
            );


            /*
             * ICONO DEL SOL
             */
            const sunIcon =
                button.querySelector(
                    '[data-theme-icon-sun]'
                );

            /*
             * ICONO DE LA LUNA
             */
            const moonIcon =
                button.querySelector(
                    '[data-theme-icon-moon]'
                );


            if (sunIcon) {

                sunIcon.classList.toggle(
                    'scale-100',
                    !useDark
                );

                sunIcon.classList.toggle(
                    'rotate-0',
                    !useDark
                );

                sunIcon.classList.toggle(
                    'opacity-100',
                    !useDark
                );

                sunIcon.classList.toggle(
                    'scale-75',
                    useDark
                );

                sunIcon.classList.toggle(
                    'rotate-90',
                    useDark
                );

                sunIcon.classList.toggle(
                    'opacity-0',
                    useDark
                );
            }


            if (moonIcon) {

                moonIcon.classList.toggle(
                    'scale-75',
                    !useDark
                );

                moonIcon.classList.toggle(
                    '-rotate-90',
                    !useDark
                );

                moonIcon.classList.toggle(
                    'opacity-0',
                    !useDark
                );

                moonIcon.classList.toggle(
                    'scale-100',
                    useDark
                );

                moonIcon.classList.toggle(
                    'rotate-0',
                    useDark
                );

                moonIcon.classList.toggle(
                    'opacity-100',
                    useDark
                );
            }

        });

    };


    /*
     * Sincronizar el tema al cargar
     */
    applyTheme(
        root.classList.contains('dark')
            ? 'dark'
            : 'light',
        false
    );


    /*
     * Cambiar tema con los botones
     */
    toggles.forEach((button) => {

        button.addEventListener('click', () => {

            const nextTheme =
                root.classList.contains('dark')
                    ? 'light'
                    : 'dark';

            applyTheme(nextTheme);

        });

    });

})();