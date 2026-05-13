import { afterNextRender, Component } from '@angular/core';

/** Snippet officiel équivalent Tunav_frontend : embed Chatbase (même chatbotId). */
const CHATBASE_BOOTSTRAP = `
(function(){
  if(!window.chatbase || window.chatbase("getState") !== "initialized") {
    window.chatbase=(...arguments)=>{
      if(!window.chatbase.q){
        window.chatbase.q=[]
      }
      window.chatbase.q.push(arguments)
    };
    window.chatbase=new Proxy(window.chatbase,{
      get(target,prop){
        if(prop==="q"){ return target.q }
        return (...args)=>target(prop,...args)
      }
    })
  }

  const onLoad=function(){
    const script=document.createElement("script");
    script.src="https://www.chatbase.co/embed.min.js";
    script.id="f0d3oyMrl3n7digplpnOD";
    script.setAttribute("chatbotId", "f0d3oyMrl3n7digplpnOD");
    document.body.appendChild(script);
  };

  if(document.readyState==="complete"){
    onLoad()
  }else{
    window.addEventListener("load",onLoad)
  }
})();`;

@Component({
  selector: 'app-chat-bot',
  standalone: true,
  templateUrl: './chat-bot.component.html',
  styleUrl: './chat-bot.component.scss',
})
export class ChatBotComponent {
  showChat = false;

  constructor() {
    afterNextRender(() => this.injectChatbase());
  }

  private injectChatbase(): void {
    const script = document.createElement('script');
    script.text = CHATBASE_BOOTSTRAP;
    document.body.appendChild(script);
  }

  toggleChat(): void {
    const iframe = document.querySelector(
      'iframe[src*="chatbase"]'
    ) as HTMLElement | null;
    if (iframe) {
      this.showChat = !this.showChat;
      iframe.style.display = this.showChat ? 'block' : 'none';
    }
  }
}
