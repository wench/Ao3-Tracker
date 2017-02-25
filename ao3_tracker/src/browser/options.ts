namespace Ao3Track {
    let islogin = true;

    jQuery(() => {
        const $ = jQuery;

        let $html = $(document.documentElement);
        let $loginorcreate = $("#loginorcreate");
        let $loggedin = $("#loggedin");

        function switchToLoggedIn(username: string)
        {
            $loggedin.find("#loggedinname").text(username);
            $loggedin.find("button[name=logoutbutton]").prop("disabled",false);
            $loginorcreate.find("#formfields").prop("disabled",true);  
            $html.addClass("loggedin").removeClass("formcreateuser formloginuser waiting");
        }
        function switchToLoginUser() 
        {
            islogin = true;
            $loggedin.find("button[name=logoutbutton]").prop("disabled",true);
            $loginorcreate.find("button[name=loginbutton]").prop("disabled",true);              
            $loginorcreate.find("button[name=createbutton]").prop("disabled",false);              
            $loginorcreate.find("#formfields").prop("disabled",false);              
            $loginorcreate.find("#createfields").prop("disabled",true);  
            $html.addClass("formloginuser").removeClass("formcreateuser loggedin waiting");
        }
        function switchToCreateUser() 
        {
            islogin = false;
            $loggedin.find("button[name=logoutbutton]").prop("disabled",true);           
            $loginorcreate.find("button[name=loginbutton]").prop("disabled",false);              
            $loginorcreate.find("button[name=createbutton]").prop("disabled",true);              
            $loginorcreate.find("#formfields").prop("disabled",false);              
            $loginorcreate.find("#createfields").prop("disabled",false);  
            $html.addClass("formcreateuser").removeClass("formloginuser loggedin waiting");
        }

        UserName((username) => {
            if (username !== "") {
                switchToLoggedIn(username);
            }
            else {
                switchToLoginUser();
            }
        });

        $loggedin.find("button[name=logoutbutton]").on("click", (event) => {
            let $button = $(event.target);
            $button.prop("disabled",true);
            UserLogout((result)=> {
                switchToLoginUser();                
            });
        });

        $loginorcreate.find("button[name=loginbutton]").on("click", (event) => {
            switchToLoginUser();
        });

        $loginorcreate.find("button[name=createbutton]").on("click", (event) => {
            switchToCreateUser();
        });

        $loginorcreate.on("submit", (event) => {
            event.preventDefault();
            let $username = $loginorcreate.find('input[name=username]');
            let $password = $loginorcreate.find('input[name=password]');
            let $verify = $loginorcreate.find('input[name=verify]');
            let $email = $loginorcreate.find('input[name=email]');
            $loginorcreate.find('.errors').text('');

            let input = {
                username: $username.val() || '',
                password: $password.val() || '',
                verify: islogin ? undefined : $verify.val() || '',
                email: islogin ? undefined : $email.val() || ''
            };

            let failed = false;
            if (input.username === '' && (failed = true)) { $username.next('.errors').text('You must enter a username'); }
            if (input.password === '' && (failed = true)) { $password.next('.errors').text('You must enter a password'); }
            if (!islogin) {
                if (input.password !== input.verify && (failed = true)) { $verify.next('.errors').text('Passwords do not match'); }
            }

            if (failed) { return; }

            $loginorcreate.find("#formfields").prop("disabled",true);            

            let callback = (errors  :{[key:string]:string}) => {
                if (errors === null) {
                    $loginorcreate.find('button:submit').next('.errors').text('Failed to contact server');
                } else if (Object.keys(errors).length === 0) {
                    switchToLoggedIn(input.username);
                } else {
                    for (let key in errors) {
                        $loginorcreate.find("input[name='" + key + "']").next('.errors').text(errors[key]);
                    }
                }
                $loginorcreate.find("#formfields").prop("disabled",false);            
            };

            if (islogin) UserLogin(input, callback);
            else UserCreate(input, callback);

        });
    });
}
