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
        toggleButton.setAttribute('aria-label', showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña');
    });
})();

(() => {
    const dismissToast = (toast) => {
        if (!toast || toast.dataset.closing === 'true') return;
        toast.dataset.closing = 'true';

        toast.classList.remove('translate-x-0', 'scale-100', 'opacity-100');
        toast.classList.add('translate-x-6', 'scale-95', 'opacity-0');

        window.setTimeout(() => toast.remove(), 300);
    };

    document.querySelectorAll('[data-toast]').forEach((toast) => {
        const closeButton = toast.querySelector('[data-toast-close]');
        const progress = toast.querySelector('[data-toast-progress]');

        requestAnimationFrame(() => {
            requestAnimationFrame(() => {
                toast.classList.remove('translate-x-6', 'scale-95', 'opacity-0');
                toast.classList.add('translate-x-0', 'scale-100', 'opacity-100');
                progress?.classList.add('scale-x-0');
            });
        });

        closeButton?.addEventListener('click', () => dismissToast(toast));
        window.setTimeout(() => dismissToast(toast), 4500);
    });
})();

(() => {
    const root = document.documentElement;
    const toggles = document.querySelectorAll('[data-theme-toggle]');

    if (!toggles.length) return;

    const applyTheme = (theme, persist = true) => {
        const useDark = theme === 'dark';
        root.classList.toggle('dark', useDark);
        root.style.colorScheme = useDark ? 'dark' : 'light';

        if (persist) {
            localStorage.setItem('kyro-theme', theme);
        }

        toggles.forEach((button) => {
            button.setAttribute('aria-label', useDark ? 'Activar modo claro' : 'Activar modo oscuro');
            button.setAttribute('title', useDark ? 'Cambiar a modo claro' : 'Cambiar a modo oscuro');

            // Sincroniza también los iconos por JS. Las clases dark: siguen siendo
            // la fuente principal del estilo, pero esto evita que el icono quede
            // desfasado si el CSS se estaba regenerando mientras se hizo clic.
            const sunIcon = button.querySelector('[data-theme-icon-sun]');
            const moonIcon = button.querySelector('[data-theme-icon-moon]');

            if (sunIcon) {
                sunIcon.classList.toggle('scale-100', !useDark);
                sunIcon.classList.toggle('rotate-0', !useDark);
                sunIcon.classList.toggle('opacity-100', !useDark);
                sunIcon.classList.toggle('scale-75', useDark);
                sunIcon.classList.toggle('rotate-90', useDark);
                sunIcon.classList.toggle('opacity-0', useDark);
            }

            if (moonIcon) {
                moonIcon.classList.toggle('scale-75', !useDark);
                moonIcon.classList.toggle('-rotate-90', !useDark);
                moonIcon.classList.toggle('opacity-0', !useDark);
                moonIcon.classList.toggle('scale-100', useDark);
                moonIcon.classList.toggle('rotate-0', useDark);
                moonIcon.classList.toggle('opacity-100', useDark);
            }
        });
    };

    applyTheme(root.classList.contains('dark') ? 'dark' : 'light', false);

    toggles.forEach((button) => {
        button.addEventListener('click', () => {
            const nextTheme = root.classList.contains('dark') ? 'light' : 'dark';
            applyTheme(nextTheme);
        });
    });
})();
