namespace Ao3Track {
    jQuery(() => {
        const $ = jQuery;

        let $login = $("#login");
        $login.on("submit",(event) => {
            event.preventDefault();
            let $username = $login.find('input[name=username]');
            let $password = $login.find('input[name=password]');
            $login.find('.errors').text('');

            let input = {
                username: $username.val() || '',
                password: $password.val() || ''
            };

            let failed = false;
            if (input.username === '' && (failed = true)) { $username.next('.errors').text('You must enter a username'); }
            if (input.password === '' && (failed = true)) { $password.next('.errors').text('You must enter a password'); }

            if (failed) { return; }

            UserLogin(input, (errors) => {
                if (errors === null) {
                    $login.find('input:submit').next('.errors').text('Failed to contact server');
                } else if (Object.keys(errors).length === 0) {
                    $login.find('input:submit').next('.errors').text('Sucessfully logged in. Cloud sync enabled.');
                } else {
                    for (let key in errors) {
                        $login.find("input[name='" + key + "']").next('.errors').text(errors[key]);
                    }
                }
            });
        });

        let $create = $("#create");
        $create.on("submit", (event) => {
            event.preventDefault();
            let $username = $create.find('input[name=username]');
            let $password = $create.find('input[name=password]');
            let $verify = $create.find('input[name=verify]');
            let $email = $create.find('input[name=email]');
            $create.find('.errors').text('');

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

            UserCreate(input, (errors) => {
                if (errors === null) {
                    $create.find('input:submit').next('.errors').text('Failed to contact server');
                } else if (Object.keys(errors).length === 0) {
                    $create.find('input:submit').next('.errors').text('User created. Cloud sync enabled.');
                } else {
                    for (let key in errors) {
                        $create.find("input[name='" + key + "']").next('.errors').text(errors[key]);
                    }
                }
            });

        });
    });
}
