// ==UserScript==
// @name         BakaSaveYourMyimaths
// @namespace    http://tampermonkey.net/
// @version      2024-09-23
// @description  做题原神2.0
// @author       You
// @match        https://app.myimaths.com/myportal/student/my_homework
// @icon         https://raw.githubusercontent.com/Arrokoth486958/cirno-is-baka/refs/heads/master/src/cirno.png
// @require      https://code.jquery.com/jquery-2.1.4.min.js
// @require      https://cdn.jsdelivr.net/npm/toastify-js
// @grant        GM.xmlHttpRequest
// @grant        GM_xmlhttpRequest
// ==/UserScript==

(function() {
    'use strict';

    const GM__xmlHttpRequest = ("undefined" != typeof (GM_xmlhttpRequest))?GM_xmlhttpRequest:GM.xmlHttpRequest;

    const styles = $(`<link rel="stylesheet" type="text/css" href="https://cdn.jsdelivr.net/npm/toastify-js/src/toastify.min.css"><style>.baka {position:absolute;right:10px;bottom:10px;width:200px;filter: drop-shadow(2px 4px 6px #00000050);cursor:pointer;} .baka:active {scale: 0.9;}</style>`)
    
    const baka = $(`<img src='https://raw.githubusercontent.com/Arrokoth486958/cirno-is-baka/refs/heads/master/src/cirno_original.png' alt='Baka' class='baka'>`);

    let toggle = false, originalTexts = [];

    $("head").append(styles);$("body").append(baka)
    
    // toggle hack mode
    baka.on('click', () => {
        const links = $(".primary.btn.btn-m");
        toggle = !toggle;
        if(toggle) {
            links.each((index, element) => {originalTexts.push($(element).html());
                $(element).html(originalTexts[index].replace("Start worksheet", "Finish").replace("Start homework", "Finish"));});
            links.on('click', CRACK);
            Toastify({text: "Hack Mode Enabled!",close: true}).showToast();
        } else {
            links.each((index, element) => $(element).html(originalTexts[index]));
            originalTexts.length = 0;
            links.off('click', CRACK);
            Toastify({text: "Hack Mode Disabled!",close: true}).showToast();
        }
    })

    // Crack Assignment
    const CRACK = (e) => {
        event.preventDefault();
        Toastify({text: `fetching: ${e.target.href}`,close: true}).showToast();
        GM__xmlHttpRequest({        // 获取原页面
            method: 'GET', url: e.target.href, anonymous: false,
            onabort: console.error, onerror: console.error, ontimeout: console.error,
            onload: (e) => {
                let embedSrc = null;
                $(e.responseText).each((index, element) => {if ($(element).is('embed#player')) {embedSrc=$(element).attr('src')}});     // 拿embed player src
                if(!embedSrc) return; const eurl = new URL(embedSrc)
                console.log(`embed player src: ${embedSrc}`);
                let params = new URLSearchParams(eurl.search);
                let contentUrl = decodeURIComponent(params.get('assetHost') + params.get('contentPath') + 'content.xml');       // 重建content.xml url
                Toastify({text: `fetching content.xml...`,close: true}).showToast();
                console.log(contentUrl)
                GM__xmlHttpRequest({        // 获取content.xml
                    method: 'GET', url: contentUrl, anonymous: false, headers: {'Referrer': embedSrc},
                    onabort: console.error, onerror: console.error, ontimeout: console.error,
                    onload: (e) => {
                        const parser = new DOMParser();       // 解析xml
                        const xmlDoc = parser.parseFromString(e.responseText.replace(/&[^;]*;/g, ''), "application/xml");
                        const questionNodes = xmlDoc.getElementsByTagName("homeworkQuestion");
                        console.log(xmlDoc.getElementsByTagName("worksheet")[0].children)
                        let Q1Score = 0, Q2Score = 0, Q3Score = 0, Q4Score = 0;
                        try {
                            Q1Score = parseInt(questionNodes[0].getAttribute('questionmarks'));
                            Q2Score = parseInt(questionNodes[1].getAttribute('questionmarks'));
                            Q3Score = parseInt(questionNodes[2].getAttribute('questionmarks'));
                            Q4Score = parseInt(questionNodes[3].getAttribute('questionmarks'));
                        } catch {}
                        let results = {
                            authCode: parseInt(params.get('authCode')),
                            taskID: parseInt(params.get('taskID')),
                            realID: parseInt(params.get('realID')),
                            studentID: parseInt(params.get('studentID')),
                            q1score: Q1Score==0?"":Q1Score,
                            q2score: Q2Score==0?"":Q2Score,
                            q3score: Q3Score==0?"":Q3Score,
                            q4score: Q4Score==0?"":Q4Score
                        };
                        Toastify({text: "calculating sCode...",close: true}).showToast();
                        let sCode = results.authCode * results.taskID;
                        if(results.q1score !== "") sCode += results.q1score * 100
                        if(results.q2score !== "") sCode += results.q2score
                        sCode *= 10000;
                        sCode += results.taskID * results.taskID;
                        results.sCode = sCode.toString();

                        Toastify({text: "Authenticating...",close: true}).showToast();
                        GM__xmlHttpRequest({        // Fetch authToken
                            method: 'POST', url: `https://app.myimaths.com/api/legacy/auth?taskId=${results.taskID}&realID=${results.realID}`, 
                            headers: {'Content-Type': 'application/x-www-form-urlencoded'},
                            anonymous: false, headers: {'Referrer': embedSrc},
                            onabort: console.error, onerror: console.error, ontimeout: console.error,
                            onload: (e) => {
                                let p = new URLSearchParams(e.responseText)
                                results.authToken = p.get('authToken');
                                results.time_spent = Math.floor(Math.random() * (1260000 - 600000 + 1) + 600000).toString()
                                delete results.authCode;

                                const formData = new URLSearchParams(results).toString();
                                Toastify({text: "Posting scores...",close: true}).showToast();
                                console.log(formData)

                                GM__xmlHttpRequest({        // save mark
                                    method: 'POST', url: `https://app.myimaths.com/api/legacy/save/mark?${formData}`, 
                                    headers: {'Content-Type': 'application/x-www-form-urlencoded'},
                                    anonymous: false, headers: {'Referrer': embedSrc},
                                    onabort: console.error, onerror: console.error, ontimeout: console.error,
                                    onload: (e) => {
                                        console.log(e.status)
                                        console.log(e.responseText)
                                        if(e.status === 200) {Toastify({text: "Success!",close: true}).showToast();}
                                        else Toastify({text: `Error ${e.status}!`,close: true}).showToast();
                                    }
                                })
                            }
                        })
                    },
                });
            },
        });
    }
})();