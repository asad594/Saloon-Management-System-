document.addEventListener('DOMContentLoaded', function () {
    document.addEventListener('click', function (e) {
        const toggleBtn = e.target.closest('.field-icon-right-btn, [data-toggle-password], #togglePwBtn, #toggleRegPwBtn, #toggleAddStaffPwBtn, #toggleAdminPwBtn');
        if (!toggleBtn) return;
        
        e.preventDefault();
        const wrapper = toggleBtn.closest('.field-input-wrapper, .form-input-wrapper');
        if (!wrapper) return;

        const input = wrapper.querySelector('input');
        const icon = toggleBtn.querySelector('i');

        if (input) {
            const isPw = input.type === 'password';
            input.type = isPw ? 'text' : 'password';
            if (icon) {
                if (isPw) {
                    icon.classList.remove('fa-eye');
                    icon.classList.add('fa-eye-slash');
                } else {
                    icon.classList.remove('fa-eye-slash');
                    icon.classList.add('fa-eye');
                }
            }
        }
    });
});
