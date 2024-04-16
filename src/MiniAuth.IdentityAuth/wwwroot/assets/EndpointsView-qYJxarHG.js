import{u as w,r as c,o as y,a,c as n,b as e,t as s,F as b,d as C,w as d,v as i}from"./index-9pQrDx-M.js";import{s as r}from"./service-wpK1kE-P.js";const g={class:"scrollable-container"},V={class:"table table-hover"},E={class:"table-dark"},R={class:"form-check form-switch"},$=["disabled","onUpdate:modelValue"],L={class:"form-check form-switch"},M=["disabled","onUpdate:modelValue"],T={class:"resizable"},U=["disabled","value","onUpdate:modelValue"],x=["for"],B=["disabled","onClick"],H=e("svg",{width:"20px",height:"20px",viewBox:"0 0 24 24",fill:"none",xmlns:"http://www.w3.org/2000/svg"},[e("g",{id:"System / Save"},[e("path",{id:"Vector",d:"M17 21.0002L7 21M17 21.0002L17.8031 21C18.921 21 19.48 21 19.9074 20.7822C20.2837 20.5905 20.5905 20.2843 20.7822 19.908C21 19.4806 21 18.921 21 17.8031V9.21955C21 8.77072 21 8.54521 20.9521 8.33105C20.9095 8.14 20.8393 7.95652 20.7432 7.78595C20.6366 7.59674 20.487 7.43055 20.1929 7.10378L17.4377 4.04241C17.0969 3.66374 16.9242 3.47181 16.7168 3.33398C16.5303 3.21 16.3242 3.11858 16.1073 3.06287C15.8625 3 15.5998 3 15.075 3H6.2002C5.08009 3 4.51962 3 4.0918 3.21799C3.71547 3.40973 3.40973 3.71547 3.21799 4.0918C3 4.51962 3 5.08009 3 6.2002V17.8002C3 18.9203 3 19.4796 3.21799 19.9074C3.40973 20.2837 3.71547 20.5905 4.0918 20.7822C4.5192 21 5.07899 21 6.19691 21H7M17 21.0002V17.1969C17 16.079 17 15.5192 16.7822 15.0918C16.5905 14.7155 16.2837 14.4097 15.9074 14.218C15.4796 14 14.9203 14 13.8002 14H10.2002C9.08009 14 8.51962 14 8.0918 14.218C7.71547 14.4097 7.40973 14.7155 7.21799 15.0918C7 15.5196 7 16.0801 7 17.2002V21M15 7H9",stroke:"#000000","stroke-width":"2","stroke-linecap":"round","stroke-linejoin":"round"})])],-1),N=[H],F={__name:"EndpointsView",setup(D){const{t:u}=w();c("EndPoints");const h=c([]),p=c([]),f=async()=>{h.value=await r.get("api/getAllEndpoints"),p.value=await r.get("api/getRoles")},k=async l=>{confirm(u("please_confirm"))&&await r.post("api/saveEndpoint",l).then(()=>{alert(u("updated_successfully"))})};return y(async()=>{await f()}),(l,P)=>(a(),n("div",g,[e("table",V,[e("thead",null,[e("tr",E,[e("th",null,s(l.$t("Name")),1),e("th",null,s(l.$t("Route")),1),e("th",null,s(l.$t("Redirect")),1),e("th",null,s(l.$t("Enable")),1),e("th",null,s(l.$t("Roles")),1),e("th",null,s(l.$t("Action")),1)])]),e("tbody",null,[(a(!0),n(b,null,C(h.value,(t,v)=>(a(),n("tr",{key:v},[e("td",null,s(t.Name),1),e("td",null,s(t.Route),1),e("td",null,[e("div",R,[d(e("input",{disabled:t.Type=="miniauth",class:"form-check-input",type:"checkbox","onUpdate:modelValue":o=>t.RedirectToLoginPage=o},null,8,$),[[i,t.RedirectToLoginPage]])])]),e("td",null,[e("div",L,[d(e("input",{disabled:t.Type=="miniauth",class:"form-check-input",type:"checkbox","onUpdate:modelValue":o=>t.Enable=o},null,8,M),[[i,t.Enable]])])]),e("td",null,[e("div",T,[(a(!0),n(b,null,C(p.value,(o,_)=>(a(),n("div",{class:"form-check",key:_},[d(e("input",{disabled:t.Type=="miniauth"||o.Enable==!1,class:"role_checkbox form-check-input",type:"checkbox",value:o.Id,"onUpdate:modelValue":m=>t.Roles=m},null,8,U),[[i,t.Roles]]),e("label",{class:"form-check-label",for:"role_"+_},s(o.Name),9,x)]))),128))])]),e("td",null,[e("button",{disabled:t.Type=="miniauth",class:"btn",onClick:o=>k(t)},N,8,B)])]))),128))])])]))}};export{F as default};
