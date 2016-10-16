$(function () {
    $("#login").submit(function (event) {
        event.preventDefault();
        let $form = $(this);
        let $username = $(this).find('input[name=username]');
        let $password = $(this).find('input[name=password]');
        $(this).find('.errors').text('');

        let input = {
            username: $username.val() || '',
            password: $password.val() || ''
        };

        let failed = false;
        if (input.username === '' && (failed = true)) { $username.next('.errors').text('You must enter a username'); }
        if (input.password === '' && (failed = true)) { $password.next('.errors').text('You must enter a password'); }

        if (failed) { return; }

        let req: UserLoginMessage = { type: "USER_LOGIN", data: input };
        chrome.runtime.sendMessage(req, function (errors: UserLoginMessageResponse) {
            if (errors === null) {
                $form.find('input:submit').next('.errors').text('Failed to contact server');
            } else if (Object.keys(errors).length === 0) {
                $form.find('input:submit').next('.errors').text('Sucessfully logged in. Cloud sync enabled.');
            } else {
                for (let key in errors) {
                    $form.find("input[name='" + key + "']").next('.errors').text(errors[key]);
                }
            }
        });
    });

    $("#create").submit(function (event) {
        event.preventDefault();
        let $form = $(this);
        let $username = $form.find('input[name=username]');
        let $password = $form.find('input[name=password]');
        let $verify = $form.find('input[name=verify]');
        let $email = $form.find('input[name=email]');
        $(this).find('.errors').text('');

        let input = {
            username: $username.val() || '',
            password: $password.val() || '',
            verify: $verify.val() || '',
            email: $email.val() || ''
        };

        let failed = false;
        if (input.username === '' && (failed = true)) { $username.next('.errors').text('You must enter a username'); }
        if (input.password === '' && (failed = true)) { $password.next('.errors').text('You must enter a password'); }
        if (input.password !== input.verify && (failed = true)) { $verify.next('.errors').text('Passwords do not match'); }

        if (failed) { return; }

        let req: UserCreateMessage = { type: "USER_CREATE", data: input };
        chrome.runtime.sendMessage(req, function (errors: UserCreateMessageResponse) {
            if (errors === null) {
                $form.find('input:submit').next('.errors').text('Failed to contact server');
            } else if (Object.keys(errors).length === 0) {
                $form.find('input:submit').next('.errors').text('User created. Cloud sync enabled.');
            } else {
                for (let key in errors) {
                    $form.find("input[name='" + key + "']").next('.errors').text(errors[key]);
                }
            }
        });

    });
});
